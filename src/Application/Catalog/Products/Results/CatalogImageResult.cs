namespace Application.Catalog.Products.Results;

public sealed class CatalogImageResult
{
    public Guid AssetId { get; init; }
    public string CardUrl { get; init; } = null!;
    public string DetailUrl { get; init; } = null!;
    public string? AltText { get; init; }
    public int Version { get; init; }
}
