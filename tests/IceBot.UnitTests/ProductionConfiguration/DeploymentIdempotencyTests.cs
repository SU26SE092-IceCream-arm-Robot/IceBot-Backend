using Application.EdgeIntegration.Abstractions;
using Application.Inventory.Abstractions;
using Application.ProductionConfiguration;
using Application.ProductionConfiguration.Deployments;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Readiness;
using Application.ProductionConfiguration.Readiness.Services;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.Services;
using Application.RobotConfiguration.Storage.Abstractions;
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
            idempotencyKey, DateTimeOffset.UtcNow, validationReportChecksum: "legacy");
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
            ReadinessGuard(),
            new FullEdgeReleaseBundleService(storage));

        var result = await handler.HandleAsync(new DeployFullEdgeConfigurationCommand
        {
            UserContext = TestData.SystemAdmin(),
            KioskId = kioskId,
            ConfigurationReleaseId = release.Id,
            KioskExecutionEndpointId = endpointId,
            IdempotencyKey = idempotencyKey
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
            validationReportChecksum: "legacy");
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
            ReadinessGuard());

        var result = await handler.HandleAsync(new DeployLowCostArtifactSetCommand
        {
            UserContext = TestData.SystemAdmin(),
            KioskId = kioskId,
            ConfigurationReleaseId = release.Id,
            KioskExecutionEndpointId = endpointId,
            IdempotencyKey = idempotencyKey,
            Selections = [new DeployLowCostArtifactSelection(routeId, programId)]
        });

        Assert.True(result.Succeeded);
        Assert.Equal(deployment.Id, result.Data!.Id);
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

    private static ProductionInventoryReadinessGuard ReadinessGuard() => new(
        Substitute.For<IInventoryReadinessEvaluator>(),
        Options.Create(new InventoryReadinessPolicyOptions()));
}
