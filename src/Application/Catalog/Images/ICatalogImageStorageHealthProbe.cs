namespace Application.Catalog.Images;

public interface ICatalogImageStorageHealthProbe
{
    Task EnsureReadyAsync(CancellationToken cancellationToken = default);
}
