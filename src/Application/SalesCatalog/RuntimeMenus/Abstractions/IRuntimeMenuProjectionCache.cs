using Application.SalesCatalog.RuntimeMenus.Results;

namespace Application.SalesCatalog.RuntimeMenus.Abstractions;

public interface IRuntimeMenuProjectionCache
{
    Task<RuntimeMenuCachedProjection> GetOrCreateAsync(
        Guid kioskId,
        Func<CancellationToken, Task<RuntimeMenuProjection>> factory,
        CancellationToken cancellationToken = default);

    Task InvalidateAsync(Guid kioskId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
