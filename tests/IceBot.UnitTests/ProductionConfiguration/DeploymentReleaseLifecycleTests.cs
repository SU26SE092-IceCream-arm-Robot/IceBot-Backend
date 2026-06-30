using Application.EdgeIntegration.Abstractions;
using Application.ProductionConfiguration;
using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.Commands;
using Microsoft.Extensions.Options;
using NSubstitute;
using IceBot.UnitTests.TestSupport;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class DeploymentReleaseLifecycleTests
{
    [Fact]
    public async Task NormalLowCostDeploy_RejectsRetiredRelease()
    {
        var release = TestData.RetiredRelease(Guid.NewGuid());
        var store = Substitute.For<IProductionConfigurationStore>();
        store.GetPublishedReleaseForDeploymentAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(release);
        var handler = CreateHandler(store);

        var result = await handler.HandleAsync(Command(release.Id, rollbackTargetId: null));

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("retired releases are available only through rollback", result.Message);
        await store.DidNotReceive().GetEndpointForDeploymentAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackLowCostDeploy_AllowsRetiredReleasePastLifecycleGate()
    {
        var release = TestData.RetiredRelease(Guid.NewGuid());
        var store = Substitute.For<IProductionConfigurationStore>();
        store.GetPublishedReleaseForDeploymentAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(release);
        store.GetEndpointForDeploymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Devices.Entities.KioskExecutionEndpoint?)null);
        var handler = CreateHandler(store);

        var result = await handler.HandleAsync(Command(release.Id, Guid.NewGuid()));

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Kiosk execution endpoint not found.", result.Message);
        await store.Received(1).GetEndpointForDeploymentAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    private static DeployLowCostArtifactSetCommandHandler CreateHandler(IProductionConfigurationStore store) =>
        new(
            store,
            Substitute.For<IEdgeCommandStore>(),
            Options.Create(new LowCostControllerCapacityOptions
            {
                MaxArtifactCount = 10,
                MaxArtifactStorageBytes = 1024 * 1024
            }),
            Substitute.For<IEdgeCommandWakeUpPublisher>());

    private static DeployLowCostArtifactSetCommand Command(Guid releaseId, Guid? rollbackTargetId) => new()
    {
        UserContext = TestData.SystemAdmin(),
        KioskId = Guid.NewGuid(),
        ConfigurationReleaseId = releaseId,
        KioskExecutionEndpointId = Guid.NewGuid(),
        IdempotencyKey = Guid.NewGuid().ToString("N"),
        Selections = [new DeployLowCostArtifactSelection(Guid.NewGuid(), Guid.NewGuid())],
        RollbackTargetDeploymentId = rollbackTargetId
    };
}
