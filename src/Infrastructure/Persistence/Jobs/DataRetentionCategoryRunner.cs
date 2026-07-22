namespace Infrastructure.Persistence.Jobs;

public sealed record DataRetentionPurgeFailure(string Category, string Error);

public static class DataRetentionCategoryRunner
{
    public static async Task<int> RunAsync(
        string category,
        Func<Task<int>> purge,
        ICollection<DataRetentionPurgeFailure> failures,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await purge();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            failures.Add(new DataRetentionPurgeFailure(category, exception.Message));
            return 0;
        }
    }
}
