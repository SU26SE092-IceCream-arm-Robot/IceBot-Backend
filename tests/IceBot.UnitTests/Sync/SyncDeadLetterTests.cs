using Application.Sync;
using Application.Sync.Abstractions;
using Domain.Sync.DeadLetters;
using Domain.Sync.Ingestion;
using Domain.Common;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using NSubstitute;

namespace IceBot.UnitTests.Sync;

public sealed class SyncDeadLetterTests
{
    [Fact]
    public async Task List_InvalidStatus_IsRejectedBeforePersistenceQuery()
    {
        var store = Substitute.For<ISyncDeadLetterStore>();
        var handler = new ListSyncDeadLettersQueryHandler(store);

        var result = await handler.HandleAsync(new ListSyncDeadLettersQuery(
            new() { IsSystemAdmin = true }, "not-a-status", null, 1, 20));

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        await store.DidNotReceive().ListAsync(
            Arg.Any<SyncDeadLetterStatus?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RetryFailure_ReturnsItemToOpen_AndPreservesExplicitResolutionBoundary()
    {
        var item = new SyncDeadLetter
        {
            EventType = "ExecutionReport.ProductionExecution",
            PayloadJson = "{}",
            ErrorMessage = "Initial failure",
            FailedAt = DateTimeOffset.UtcNow
        };

        item.BeginRetry();
        Assert.Equal(SyncDeadLetterStatus.RetryInProgress, item.Status);
        item.ReturnToOpen("Retry failed");
        Assert.Equal(SyncDeadLetterStatus.Open, item.Status);

        item.Ignore(Guid.NewGuid(), DateTimeOffset.UtcNow, "Not replayable business evidence");
        Assert.Equal(SyncDeadLetterStatus.Ignored, item.Status);
        Assert.Throws<DomainRuleException>(() => item.BeginRetry());
    }

    [Fact]
    public void Inbox_ManualRetry_ResetsTerminalRetryState()
    {
        var inbox = new SyncEventInbox { Status = SyncEventStatus.DeadLettered, ProcessingAttempts = 5 };
        inbox.PrepareManualRetry(DateTimeOffset.UtcNow);
        Assert.Equal(SyncEventStatus.Failed, inbox.Status);
        Assert.Equal(0, inbox.ProcessingAttempts);
    }
}
