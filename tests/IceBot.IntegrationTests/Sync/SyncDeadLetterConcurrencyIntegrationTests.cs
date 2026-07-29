using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Sync;

namespace IceBot.IntegrationTests.Sync;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class SyncDeadLetterConcurrencyIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task SerializedMutation_DoesNotAllowConcurrentEntryForSameDeadLetter()
    {
        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var firstStore = new SyncDeadLetterStore(firstContext);
        var secondStore = new SyncDeadLetterStore(secondContext);
        var deadLetterId = Guid.NewGuid();
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = firstStore.ExecuteSerializedAsync(deadLetterId, async cancellationToken =>
        {
            firstEntered.SetResult();
            await releaseFirst.Task.WaitAsync(cancellationToken);
            return 1;
        });
        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var second = secondStore.ExecuteSerializedAsync(deadLetterId, cancellationToken =>
        {
            secondEntered.SetResult();
            return Task.FromResult(2);
        });

        await Task.Delay(200);
        Assert.False(secondEntered.Task.IsCompleted);

        releaseFirst.SetResult();
        var results = await Task.WhenAll(first, second);
        Assert.Equal(new[] { 1, 2 }, results);
        Assert.True(secondEntered.Task.IsCompleted);
    }
}
