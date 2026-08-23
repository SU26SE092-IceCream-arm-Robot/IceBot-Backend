using Application.Catalog.Images;
using Application.Shared.Concurrency;
using Infrastructure.Concurrency;

namespace Infrastructure.Catalog.Images;

public sealed class PostgresCatalogImageMutationCoordinator(PostgresAdvisoryLockManager locks)
    : ICatalogImageMutationCoordinator
{
    public async Task<T> ExecuteAsync<T>(
        Guid productId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var identity = TechnicalResourceMutationIdentity.Product(productId);
        await using var mutationLock = await locks.AcquireAsync(identity.AdvisoryLockKey, cancellationToken);
        return await action(cancellationToken);
    }
}
