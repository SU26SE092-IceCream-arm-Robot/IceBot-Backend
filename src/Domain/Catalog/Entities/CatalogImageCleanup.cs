using Domain.Common;

namespace Domain.Catalog.Entities;

public sealed class CatalogImageCleanup : BusinessEntity
{
    public Guid CatalogImageAssetId { get; set; }
    public string PublicIdSnapshot { get; set; } = null!;
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? LastErrorCode { get; set; }

    public CatalogImageAsset CatalogImageAsset { get; set; } = null!;
}
