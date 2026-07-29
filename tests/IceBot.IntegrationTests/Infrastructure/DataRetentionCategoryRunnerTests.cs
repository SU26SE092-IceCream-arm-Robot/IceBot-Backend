using Infrastructure.Persistence.Jobs;

namespace IceBot.IntegrationTests.Infrastructure;

public sealed class DataRetentionCategoryRunnerTests
{
    [Fact]
    public async Task FailureInOneCategory_DoesNotPreventLaterCategories()
    {
        var failures = new List<DataRetentionPurgeFailure>();
        var laterCategoryRan = false;

        var failedCount = await DataRetentionCategoryRunner.RunAsync(
            "device_events",
            () => Task.FromException<int>(new InvalidOperationException("database timeout")),
            failures);
        var succeededCount = await DataRetentionCategoryRunner.RunAsync(
            "operation_logs",
            () =>
            {
                laterCategoryRan = true;
                return Task.FromResult(4);
            },
            failures);

        Assert.Equal(0, failedCount);
        Assert.Equal(4, succeededCount);
        Assert.True(laterCategoryRan);
        var failure = Assert.Single(failures);
        Assert.Equal("device_events", failure.Category);
    }

    [Fact]
    public async Task CallerCancellation_IsNotConvertedToCategoryFailure()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var failures = new List<DataRetentionPurgeFailure>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            DataRetentionCategoryRunner.RunAsync(
                "heartbeats",
                () => Task.FromCanceled<int>(cancellation.Token),
                failures,
                cancellation.Token));

        Assert.Empty(failures);
    }
}
