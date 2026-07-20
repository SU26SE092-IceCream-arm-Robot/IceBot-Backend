using Application.Shared.Concurrency;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Concurrency;

public sealed class PostgresTechnicalResourceMutationCoordinator(IceBotDbContext dbContext)
    : ITechnicalResourceMutationCoordinator
{
    public async Task<T> ExecuteAsync<T>(
        IReadOnlyCollection<TechnicalResourceMutationIdentity> resources,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        if (resources.Count == 0)
            throw new InvalidOperationException("At least one technical resource mutation identity is required.");

        var ownsTransaction = dbContext.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            foreach (var resource in TechnicalResourceMutationIdentity.OrderForLocking(resources))
            {
                if (string.IsNullOrWhiteSpace(resource.ResourceKey) || string.IsNullOrWhiteSpace(resource.ResourceType))
                    throw new InvalidOperationException("Technical resource mutation identity is invalid.");
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtextextended({resource.AdvisoryLockKey}, 0))",
                    cancellationToken);
            }

            var result = await action(cancellationToken);
            if (ownsTransaction) await transaction!.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            if (ownsTransaction) await transaction!.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
