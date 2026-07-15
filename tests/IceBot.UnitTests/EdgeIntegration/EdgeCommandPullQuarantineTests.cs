using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.CommandDelivery.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Domain.Devices.ExecutionEndpoints;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using IceBot.UnitTests.TestSupport;
using NSubstitute;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class EdgeCommandPullQuarantineTests
{
    [Fact]
    public async Task InvalidDeploymentPayloadIsQuarantinedWithoutBlockingLaterCommand()
    {
        var kioskId = Guid.NewGuid();
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kioskId,
            "EDGE-01",
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        var credential = (ExecutionEndpointCredentialBinding)Activator.CreateInstance(
            typeof(ExecutionEndpointCredentialBinding), nonPublic: true)!;
        TestData.SetProperty(credential, nameof(ExecutionEndpointCredentialBinding.Status),
            ExecutionEndpointCredentialBindingStatus.Active);
        TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.CredentialBinding), credential);
        TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.Status), KioskExecutionEndpointStatus.Active);

        var invalid = EdgeCommand.Create(
            EdgeCommandType.DeployConfiguration,
            kioskId,
            endpoint.Id,
            "{invalid-json",
            DateTimeOffset.UtcNow,
            deploymentId: Guid.NewGuid(),
            deploymentKind: DeploymentCommandTargetKind.FullEdgeConfiguration);
        var valid = EdgeCommand.Create(
            EdgeCommandType.DeployConfiguration,
            kioskId,
            endpoint.Id,
            "{}",
            DateTimeOffset.UtcNow,
            deploymentId: Guid.NewGuid(),
            deploymentKind: DeploymentCommandTargetKind.FullEdgeConfiguration);

        var store = Substitute.For<IEdgeCommandStore>();
        store.GetEndpointForCommandAuthAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        store.ListDispatchableAsync(kioskId, endpoint.Id, Arg.Any<int>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns([invalid, valid]);
        var handler = new PullEdgeCommandsCommandHandler(
            store,
            new ArtifactCommandPayloadEnricher(Substitute.For<IArtifactObjectStorage>()));

        var result = await handler.HandleAsync(new PullEdgeCommandsCommand
        {
            KioskId = kioskId,
            EndpointId = endpoint.Id,
            MaxCommands = 20
        });

        Assert.True(result.Succeeded);
        Assert.Single(result.Data!.Commands);
        Assert.Equal(valid.Id, result.Data.Commands.Single().CommandId);
        Assert.Equal(EdgeCommandStatus.DeliveryFailed, invalid.Status);
        Assert.Equal("InvalidDurablePayload", invalid.DeliveryAttempts.Single().ResponseCode);
        Assert.Equal(EdgeCommandStatus.Delivered, valid.Status);
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
