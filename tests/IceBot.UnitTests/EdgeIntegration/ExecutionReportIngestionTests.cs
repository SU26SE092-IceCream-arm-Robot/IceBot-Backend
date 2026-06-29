using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Commands;
using Application.EdgeIntegration.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.Sync.Entities;
using NSubstitute;
using Application.Abstractions.Realtime;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class ExecutionReportIngestionTests
{
    [Fact]
    public async Task HandleAsync_ReturnsDuplicateWithoutReapplyingExistingSourceEvent()
    {
        var kioskId = Guid.NewGuid();
        var endpoint = ActiveFullEdgeEndpoint(kioskId);
        var sourceEventId = Guid.NewGuid();
        var store = Substitute.For<IExecutionReportStore>();
        store.GetEndpointForReportAuthAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        store.ExecuteReportIngestionAsync(
                sourceEventId,
                Arg.Any<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>()(CancellationToken.None));
        store.GetSyncEventByEventIdAsync(sourceEventId, Arg.Any<CancellationToken>())
            .Returns(new SyncEventInbox { EventId = sourceEventId });
        var handler = new IngestExecutionReportCommandHandler(
            store,
            Substitute.For<IRealtimeNotificationPublisher>());

        var result = await handler.HandleAsync(new IngestExecutionReportCommand
        {
            KioskId = kioskId,
            EndpointId = endpoint.Id,
            CommandId = Guid.NewGuid(),
            SourceEventId = sourceEventId,
            SequenceNumber = 1,
            EdgeCreatedAt = DateTimeOffset.UtcNow,
            ReportType = "Deployment",
            Status = "Installed",
            DeploymentId = Guid.NewGuid()
        });

        Assert.True(result.Succeeded);
        Assert.True(result.Data!.Duplicate);
        Assert.False(result.Data.Applied);
        await store.DidNotReceive().GetCommandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static KioskExecutionEndpoint ActiveFullEdgeEndpoint(Guid kioskId)
    {
        var now = DateTimeOffset.UtcNow;
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kioskId,
            "EDGE-01",
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        endpoint.ProvisionCredential("certificate-fingerprint", now);
        endpoint.Activate(Guid.NewGuid(), now);
        return endpoint;
    }
}
