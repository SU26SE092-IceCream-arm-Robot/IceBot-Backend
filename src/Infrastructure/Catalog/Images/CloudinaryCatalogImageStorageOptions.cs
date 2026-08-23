using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Catalog.Images;

public sealed class CloudinaryCatalogImageStorageOptions
{
    public const string SectionName = "Media:Cloudinary";

    [Required] public string CloudName { get; init; } = string.Empty;
    [Required] public string ApiKey { get; init; } = string.Empty;
    [Required] public string ApiSecret { get; init; } = string.Empty;
    // This is the sole environment namespace, for example icebot/production.
    [Required] public string RootFolder { get; init; } = string.Empty;
    [Range(1, 20 * 1024 * 1024)] public int MaxUploadBytes { get; init; } = 5 * 1024 * 1024;
}
