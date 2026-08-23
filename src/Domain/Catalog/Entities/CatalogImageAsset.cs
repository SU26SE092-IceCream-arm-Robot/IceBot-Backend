using Domain.Catalog.Enums;
using Domain.Common;

namespace Domain.Catalog.Entities;

/// <summary>Immutable Cloudinary metadata for an image owned by the catalog.</summary>
public sealed class CatalogImageAsset : BusinessEntity
{
    public string Provider { get; set; } = null!;
    public string ProviderAssetId { get; set; } = null!;
    public string PublicId { get; set; } = null!;
    public string DeliveryUrl { get; set; } = null!;
    public int Version { get; set; }
    public string Format { get; set; } = null!;
    public int Width { get; set; }
    public int Height { get; set; }
    public long Bytes { get; set; }
    public CatalogImageAssetStatus Status { get; set; } = CatalogImageAssetStatus.Active;
}
