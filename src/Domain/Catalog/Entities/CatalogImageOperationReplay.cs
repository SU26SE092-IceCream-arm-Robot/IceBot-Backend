using Domain.Catalog.Enums;
using Domain.Common;

namespace Domain.Catalog.Entities;

public sealed class CatalogImageOperationReplay : BusinessEntity
{
    public string ScopeKey { get; set; } = null!;
    public string OwnerType { get; set; } = null!;
    public Guid OwnerId { get; set; }
    public CatalogImageOperation Operation { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public string RequestFingerprint { get; set; } = null!;
}
