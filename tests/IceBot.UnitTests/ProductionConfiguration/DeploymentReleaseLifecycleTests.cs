using Domain.Devices.ExecutionEndpoints;
using Application.EdgeIntegration.Abstractions;
using Application.ProductionConfiguration;
using Application.ProductionConfiguration.Deployments;
using Application.ProductionConfiguration.Readiness;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Routes.Commands;
using Microsoft.Extensions.Options;
using NSubstitute;
using IceBot.UnitTests.TestSupport;
using Application.Inventory.Abstractions;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Readiness.Services;
using Application.ProductionConfiguration.Deployments.Services;
using Application.Operations.OperationLogs.Abstractions;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class DeploymentReleaseLifecycleTests
{
    [Fact]
    public async Task NormalLowCostDeploy_RejectsRetiredRelease()
    {
        var release = TestData.RetiredRelease(Guid.NewGuid());
        var deploymentStore = Substitute.For<IConfigurationDeploymentStore>();
        var releaseStore = Substitute.For<IConfigurationReleaseStore>();
        releaseStore.GetPublishedReleaseForDeploymentAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(release);
        var command = Command(release.Id, rollbackTargetId: null);
        deploymentStore.GetEndpointForDeploymentAsync(command.KioskExecutionEndpointId, Arg.Any<CancellationToken>())
            .Returns(Endpoint(command, release.OrganizationId));
        var handler = CreateHandler(deploymentStore, releaseStore);

        var result = await handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("retired releases are available only through rollback", result.Message);
        await deploymentStore.Received(1).GetEndpointForDeploymentAsync(
            command.KioskExecutionEndpointId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackLowCostDeploy_AllowsRetiredReleasePastLifecycleGate()
    {
        var release = TestData.RetiredRelease(Guid.NewGuid());
        var deploymentStore = Substitute.For<IConfigurationDeploymentStore>();
        var releaseStore = Substitute.For<IConfigurationReleaseStore>();
        releaseStore.GetPublishedReleaseForDeploymentAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(release);
        deploymentStore.GetEndpointForDeploymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint?)null);
        var handler = CreateHandler(deploymentStore, releaseStore);

        var result = await handler.HandleAsync(Command(release.Id, Guid.NewGuid()));

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Kiosk execution endpoint not found.", result.Message);
        await deploymentStore.Received(1).GetEndpointForDeploymentAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    private static DeployLowCostArtifactSetCommandHandler CreateHandler(
        IConfigurationDeploymentStore deploymentStore,
        IConfigurationReleaseStore releaseStore) =>
        new(
            deploymentStore,
            releaseStore,
            Substitute.For<IEdgeCommandStore>(),
            Options.Create(new LowCostControllerCapacityOptions
            {
                MaxArtifactCount = 10,
                MaxArtifactStorageBytes = 1024 * 1024
            }),
            Substitute.For<IEdgeCommandWakeUpPublisher>(),
            Substitute.For<IConfigurationDeploymentPreviewService>(),
            new DeploymentOperationAuditWriter(Substitute.For<IOperationLogStore>()));

    private static DeployLowCostArtifactSetCommand Command(Guid releaseId, Guid? rollbackTargetId) => new()
    {
        UserContext = TestData.SystemAdmin(),
        KioskId = Guid.NewGuid(),
        ConfigurationReleaseId = releaseId,
        KioskExecutionEndpointId = Guid.NewGuid(),
        IdempotencyKey = Guid.NewGuid().ToString("N"),
        Reason = "Test deployment request",
        Selections = [new DeployLowCostArtifactSelection(Guid.NewGuid(), Guid.NewGuid())],
        RollbackTargetDeploymentId = rollbackTargetId
    };

    private static KioskExecutionEndpoint Endpoint(
        DeployLowCostArtifactSetCommand command,
        Guid organizationId)
    {
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            command.KioskId,
            "LOW-COST",
            KioskExecutionProfile.LowCostController,
            ExecutionEndpointAuthenticationMode.SignedCommandTls);
        endpoint.Id = command.KioskExecutionEndpointId;
        TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.ControllerId), Guid.NewGuid());
        TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.Kiosk), new Domain.Tenants.Entities.Kiosk
        {
            Id = command.KioskId,
            OrganizationId = organizationId,
            StoreId = Guid.NewGuid(),
            Code = "KIOSK",
            Name = "Kiosk"
        });
        return endpoint;
    }
}
