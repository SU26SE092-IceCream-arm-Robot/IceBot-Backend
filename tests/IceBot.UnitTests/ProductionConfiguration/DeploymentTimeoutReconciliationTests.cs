using Application.EdgeIntegration.Abstractions;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Deployments.Commands;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using NSubstitute;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class DeploymentTimeoutReconciliationTests
{
    [Fact]
    public async Task UnsupportedExpiredCommand_DoesNotBlockLaterValidDeployment()
    {
        var observedAt = new DateTimeOffset(2026, 7, 21, 9, 0, 0, TimeSpan.Zero);
        var deployment = KioskConfigurationDeployment.CreatePending(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "release-checksum", 1, "deployment-key", observedAt.AddMinutes(-2), null,
            "validation-checksum", "UnprovenPhysicalBehavior", "[]");
        var unsupported = DeploymentCommand(
            Guid.NewGuid(), (DeploymentCommandTargetKind)999, observedAt);
        var valid = DeploymentCommand(
            deployment.Id, DeploymentCommandTargetKind.FullEdgeConfiguration, observedAt);
        var edgeStore = Substitute.For<IEdgeCommandStore>();
        edgeStore.ListExpiredDeploymentCommandsAsync(observedAt, 10, Arg.Any<CancellationToken>())
            .Returns([unsupported, valid]);
        var deploymentStore = Substitute.For<IConfigurationDeploymentStore>();
        deploymentStore.GetFullEdgeDeploymentForReconciliationAsync(deployment.Id, Arg.Any<CancellationToken>())
            .Returns(deployment);
        var handler = new ReconcileExpiredDeploymentCommandsCommandHandler(edgeStore, deploymentStore);

        var result = await handler.HandleAsync(observedAt, 10);

        Assert.Equal(1, result.ReconciledDeploymentCount);
        Assert.Single(result.Failures);
        Assert.Equal(unsupported.Id, result.Failures[0].CommandId);
        Assert.Equal(KioskConfigurationDeploymentStatus.Failed, deployment.Status);
        await edgeStore.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static EdgeCommand DeploymentCommand(
        Guid deploymentId,
        DeploymentCommandTargetKind kind,
        DateTimeOffset observedAt) =>
        EdgeCommand.Create(
            EdgeCommandType.DeployConfiguration,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "{}",
            observedAt.AddMinutes(-2),
            commandExpiryAt: observedAt.AddMinutes(-1),
            deploymentId: deploymentId,
            deploymentKind: kind,
            requestedCommandExpiryAt: observedAt.AddMinutes(-1));
}
