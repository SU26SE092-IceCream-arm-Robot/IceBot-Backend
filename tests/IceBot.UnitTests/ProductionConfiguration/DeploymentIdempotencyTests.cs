using Application.EdgeIntegration.Abstractions;
using Application.Inventory.Abstractions;
using Application.ProductionConfiguration;
using Application.ProductionConfiguration.Deployments;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Deployments.ReadModels;
using Application.ProductionConfiguration.Deployments.Services;
using Application.Operations.OperationLogs.Abstractions;
using Application.ProductionConfiguration.Readiness;
using Application.ProductionConfiguration.Readiness.Services;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.Shared.Wrappers;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Domain.Tenants.Entities;
using IceBot.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class DeploymentIdempotencyTests
{
    [Fact]
    public async Task FullEdgeDeploy_DoesNotBypassIneligiblePreview()
    {
        var organizationId = Guid.NewGuid();
        var kioskId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        var release = TestData.RetiredRelease(organizationId);
        var endpoint = Endpoint(endpointId, kioskId, organizationId,
            KioskExecutionProfile.FullEdge, Guid.NewGuid(), null);
        var deploymentStore = Substitute.For<IConfigurationDeploymentStore>();
        deploymentStore.GetEndpointForDeploymentAsync(endpointId, Arg.Any<CancellationToken>()).Returns(endpoint);
        var releaseStore = Substitute.For<IConfigurationReleaseStore>();
        releaseStore.GetPublishedReleaseForDeploymentAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        var preview = Substitute.For<IConfigurationDeploymentPreviewService>();
        preview.HandleAsync(Arg.Any<Application.Identity.Tokens.Claims.CurrentUserContext>(), kioskId, release.Id,
                endpointId, Arg.Any<IReadOnlyCollection<DeploymentPreviewSelection>>(),
                Arg.Any<CancellationToken>(), false)
            .Returns(ApiResult<ConfigurationDeploymentPreview>.Success(
                Preview(release, endpoint, isEligible: false)));
        var handler = new DeployFullEdgeConfigurationCommandHandler(
            deploymentStore, releaseStore, Substitute.For<IEdgeCommandStore>(),
            Substitute.For<IEdgeCommandWakeUpPublisher>(),
            new FullEdgeReleaseBundleService(Substitute.For<IArtifactObjectStorage>()),
            preview,
            AuditWriter());

        var result = await handler.HandleAsync(new DeployFullEdgeConfigurationCommand
        {
            UserContext = TestData.SystemAdmin(),
            KioskId = kioskId,
            ConfigurationReleaseId = release.Id,
            KioskExecutionEndpointId = endpointId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Reason = "Test deployment request",
            DeploymentPreviewChecksum = new string('a', 64)
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.DoesNotContain(deploymentStore.ReceivedCalls(), call =>
            call.GetMethodInfo().Name == nameof(IConfigurationDeploymentStore.ExecuteDeploymentCreationAsync));
    }

    [Fact]
    public async Task FullEdgeRetry_ReturnsExistingBeforeBundleStorageIo()
    {
        var organizationId = Guid.NewGuid();
        var kioskId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        var runtimeId = Guid.NewGuid();
        var release = TestData.RetiredRelease(organizationId);
        var endpoint = Endpoint(
            endpointId, kioskId, organizationId, KioskExecutionProfile.FullEdge, runtimeId, null);
        var idempotencyKey = Guid.NewGuid().ToString("N");
        var deployment = KioskConfigurationDeployment.CreatePending(
            kioskId, organizationId, endpointId, runtimeId, release.Id, release.ReleaseChecksum!, 1,
            idempotencyKey, DateTimeOffset.UtcNow, null, PreviewChecksum, "UnprovenPhysicalBehavior", "[]");
        var edgeCommand = DeploymentCommand(
            kioskId, endpointId, deployment.Id, DeploymentCommandTargetKind.FullEdgeConfiguration);
        var deploymentStore = Substitute.For<IConfigurationDeploymentStore>();
        deploymentStore.GetEndpointForDeploymentAsync(endpointId, Arg.Any<CancellationToken>()).Returns(endpoint);
        deploymentStore.GetFullEdgeDeploymentByIdempotencyKeyAsync(
            endpointId, idempotencyKey, Arg.Any<CancellationToken>()).Returns(deployment);
        var releaseStore = Substitute.For<IConfigurationReleaseStore>();
        releaseStore.GetPublishedReleaseForDeploymentAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        var edgeStore = Substitute.For<IEdgeCommandStore>();
        edgeStore.GetByDeploymentIdAsync(deployment.Id, Arg.Any<CancellationToken>()).Returns(edgeCommand);
        var storage = Substitute.For<IArtifactObjectStorage>();
        var handler = new DeployFullEdgeConfigurationCommandHandler(
            deploymentStore,
            releaseStore,
            edgeStore,
            Substitute.For<IEdgeCommandWakeUpPublisher>(),
            new FullEdgeReleaseBundleService(storage),
            Substitute.For<IConfigurationDeploymentPreviewService>(),
            AuditWriter());

        var result = await handler.HandleAsync(new DeployFullEdgeConfigurationCommand
        {
            UserContext = TestData.SystemAdmin(),
            KioskId = kioskId,
            ConfigurationReleaseId = release.Id,
            KioskExecutionEndpointId = endpointId,
            IdempotencyKey = idempotencyKey,
            Reason = "Test deployment retry",
            DeploymentPreviewChecksum = PreviewChecksum
        });

        Assert.True(result.Succeeded);
        Assert.Equal(deployment.Id, result.Data!.Id);
        await storage.DidNotReceive().ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await storage.DidNotReceive().ReadBytesAsync(
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
        Assert.DoesNotContain(
            deploymentStore.ReceivedCalls(),
            call => call.GetMethodInfo().Name == nameof(IConfigurationDeploymentStore.ExecuteDeploymentCreationAsync));
    }

    [Fact]
    public async Task LowCostRetry_ReturnsExistingAfterReleaseWasRetired()
    {
        var organizationId = Guid.NewGuid();
        var kioskId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        var controllerId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var release = TestData.RetiredRelease(organizationId);
        var endpoint = Endpoint(
            endpointId, kioskId, organizationId, KioskExecutionProfile.LowCostController, null, controllerId);
        var idempotencyKey = Guid.NewGuid().ToString("N");
        var deployment = ControllerArtifactSetDeployment.CreatePending(
            kioskId, organizationId, endpointId, controllerId, release.Id, release.ReleaseChecksum!, 1,
            idempotencyKey, 10, 1024 * 1024, Guid.NewGuid(), DateTimeOffset.UtcNow,
            [new ControllerArtifactSetItemSnapshot(
                routeId, programId, new string('p', 64), Guid.NewGuid(), new string('a', 64),
                "robot-artifacts/test.lua", "FAIRINO_LUA_V1", "FR5", null,
                128, 1, 1, null)],
            PreviewChecksum, "UnprovenPhysicalBehavior", "[]");
        var edgeCommand = DeploymentCommand(
            kioskId, endpointId, deployment.Id, DeploymentCommandTargetKind.LowCostArtifactSet);
        var deploymentStore = Substitute.For<IConfigurationDeploymentStore>();
        deploymentStore.GetEndpointForDeploymentAsync(endpointId, Arg.Any<CancellationToken>()).Returns(endpoint);
        deploymentStore.GetControllerDeploymentByIdempotencyKeyAsync(
            endpointId, idempotencyKey, Arg.Any<CancellationToken>()).Returns(deployment);
        var releaseStore = Substitute.For<IConfigurationReleaseStore>();
        releaseStore.GetPublishedReleaseForDeploymentAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        var edgeStore = Substitute.For<IEdgeCommandStore>();
        edgeStore.GetByDeploymentIdAsync(deployment.Id, Arg.Any<CancellationToken>()).Returns(edgeCommand);
        var handler = new DeployLowCostArtifactSetCommandHandler(
            deploymentStore,
            releaseStore,
            edgeStore,
            Options.Create(new LowCostControllerCapacityOptions
            {
                MaxArtifactCount = 10,
                MaxArtifactStorageBytes = 1024 * 1024
            }),
            Substitute.For<IEdgeCommandWakeUpPublisher>(),
            Substitute.For<IConfigurationDeploymentPreviewService>(),
            AuditWriter());

        var result = await handler.HandleAsync(new DeployLowCostArtifactSetCommand
        {
            UserContext = TestData.SystemAdmin(),
            KioskId = kioskId,
            ConfigurationReleaseId = release.Id,
            KioskExecutionEndpointId = endpointId,
            IdempotencyKey = idempotencyKey,
            Reason = "Test deployment retry",
            DeploymentPreviewChecksum = PreviewChecksum,
            Selections = [new DeployLowCostArtifactSelection(routeId, programId)]
        });

        Assert.True(result.Succeeded);
        Assert.Equal(deployment.Id, result.Data!.Id);
        Assert.DoesNotContain(
            deploymentStore.ReceivedCalls(),
            call => call.GetMethodInfo().Name == nameof(IConfigurationDeploymentStore.ExecuteDeploymentCreationAsync));
    }

    [Fact]
    public async Task Rollback_RejectsWhenClientObservedActiveDeploymentIsStale()
    {
        var organizationId = Guid.NewGuid();
        var kioskId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        var targetDeploymentId = Guid.NewGuid();
        var currentDeploymentId = Guid.NewGuid();
        var deploymentStore = Substitute.For<IConfigurationDeploymentStore>();
        var endpoint = Endpoint(
            endpointId, kioskId, organizationId, KioskExecutionProfile.FullEdge, Guid.NewGuid(), null);
        TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.ActiveConfigurationDeploymentId), currentDeploymentId);
        deploymentStore.GetConfigurationDeploymentAsync(targetDeploymentId, Arg.Any<CancellationToken>())
            .Returns(new ConfigurationDeploymentReadModel
            {
                Id = targetDeploymentId,
                Profile = ConfigurationDeploymentProfile.FullEdge,
                OrganizationId = organizationId,
                StoreId = endpoint.Kiosk.StoreId,
                KioskId = kioskId,
                KioskExecutionEndpointId = endpointId,
                ConfigurationReleaseId = Guid.NewGuid(),
                ReleaseChecksum = new string('a', 64),
                Status = ConfigurationDeploymentReadStatus.Active
            });
        deploymentStore.GetEndpointForDeploymentAsync(endpointId, Arg.Any<CancellationToken>()).Returns(endpoint);

        var fullEdge = new DeployFullEdgeConfigurationCommandHandler(
            deploymentStore,
            Substitute.For<IConfigurationReleaseStore>(),
            Substitute.For<IEdgeCommandStore>(),
            Substitute.For<IEdgeCommandWakeUpPublisher>(),
            new FullEdgeReleaseBundleService(Substitute.For<IArtifactObjectStorage>()),
            Substitute.For<IConfigurationDeploymentPreviewService>(),
            AuditWriter());
        var lowCost = new DeployLowCostArtifactSetCommandHandler(
            deploymentStore,
            Substitute.For<IConfigurationReleaseStore>(),
            Substitute.For<IEdgeCommandStore>(),
            Options.Create(new LowCostControllerCapacityOptions()),
            Substitute.For<IEdgeCommandWakeUpPublisher>(),
            Substitute.For<IConfigurationDeploymentPreviewService>(),
            AuditWriter());
        var handler = new RollbackConfigurationDeploymentCommandHandler(deploymentStore, fullEdge, lowCost);

        var result = await handler.HandleAsync(new RollbackConfigurationDeploymentCommand
        {
            UserContext = TestData.SystemAdmin(),
            KioskId = kioskId,
            TargetDeploymentId = targetDeploymentId,
            ExpectedActiveDeploymentId = Guid.NewGuid(),
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Reason = "Rollback after production incident."
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.DoesNotContain(
            deploymentStore.ReceivedCalls(),
            call => call.GetMethodInfo().Name == nameof(IConfigurationDeploymentStore.ExecuteDeploymentCreationAsync));
    }

    private static KioskExecutionEndpoint Endpoint(
        Guid endpointId,
        Guid kioskId,
        Guid organizationId,
        KioskExecutionProfile profile,
        Guid? runtimeId,
        Guid? controllerId)
    {
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kioskId,
            $"ENDPOINT-{endpointId:N}",
            profile,
            profile == KioskExecutionProfile.FullEdge
                ? ExecutionEndpointAuthenticationMode.MutualTls
                : ExecutionEndpointAuthenticationMode.SignedCommandTls);
        endpoint.Id = endpointId;
        TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.Kiosk), new Kiosk
        {
            Id = kioskId,
            OrganizationId = organizationId,
            StoreId = Guid.NewGuid(),
            Code = "KIOSK",
            Name = "Kiosk"
        });
        if (runtimeId.HasValue)
            TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.FullEdgeRuntimeId), runtimeId);
        if (controllerId.HasValue)
            TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.ControllerId), controllerId);
        return endpoint;
    }

    private static EdgeCommand DeploymentCommand(
        Guid kioskId,
        Guid endpointId,
        Guid deploymentId,
        DeploymentCommandTargetKind kind) =>
        EdgeCommand.Create(
            EdgeCommandType.DeployConfiguration,
            kioskId,
            endpointId,
            "{}",
            DateTimeOffset.UtcNow,
            deploymentId: deploymentId,
            deploymentKind: kind);

    private static ConfigurationDeploymentPreview Preview(
        ConfigurationRelease release,
        KioskExecutionEndpoint endpoint,
        bool isEligible)
    {
        var endpointPreview = new ConfigurationDeploymentEndpointPreview(
            endpoint.Id, endpoint.EndpointCode, endpoint.ExecutionProfile.ToString(), isEligible,
            isEligible ? [] : [new DeploymentPreviewBlocker("EndpointNotReady", "Endpoint is not ready.")],
            [], [], [], ["BundleInstall"], 0, 0, null, null, PreviewChecksum,
            new DeploymentValidationReport(PreviewChecksum, "UnprovenPhysicalBehavior", [], false));
        return new ConfigurationDeploymentPreview(
            release.Id, release.ReleaseChecksum!, endpoint.KioskId, false, [endpointPreview]);
    }

    private static DeploymentOperationAuditWriter AuditWriter() =>
        new(Substitute.For<IOperationLogStore>());

    private const string PreviewChecksum = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
}
