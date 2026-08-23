namespace Infrastructure.Catalog.Images;

public sealed class CatalogImageCleanupOptions
{
    public const string SectionName = "Media:Cloudinary:Cleanup";

    public bool Enabled { get; init; } = true;
    public int IntervalMinutes { get; init; } = 15;
    public int BatchSize { get; init; } = 100;
}
