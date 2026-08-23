namespace Application.Catalog.Images;

public interface ICatalogImageMutationCoordinator
{
    Task<T> ExecuteAsync<T>(
        Guid productId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}

public sealed class InlineCatalogImageMutationCoordinator : ICatalogImageMutationCoordinator
{
    public static InlineCatalogImageMutationCoordinator Instance { get; } = new();

    private InlineCatalogImageMutationCoordinator() { }

    public Task<T> ExecuteAsync<T>(
        Guid productId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default) => action(cancellationToken);
}
