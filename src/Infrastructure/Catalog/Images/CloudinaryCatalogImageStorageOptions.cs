namespace Infrastructure.Catalog.Images;

public sealed class CloudinaryCatalogImageStorageOptions
{
    public const string SectionName = "Media:Cloudinary";

    public string CloudName { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    // This is the sole environment namespace, for example icebot/production.
    public string RootFolder { get; init; } = string.Empty;
    public int MaxUploadBytes { get; init; } = 5 * 1024 * 1024;
    public int MinDimensionPixels { get; init; } = 400;
    public int MaxDimensionPixels { get; init; } = 4096;
}
