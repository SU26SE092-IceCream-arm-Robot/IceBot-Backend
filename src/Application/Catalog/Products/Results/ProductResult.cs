using Domain.Tenants.Enums;

namespace Application.Catalog.Products.Results;

public sealed class ProductResult
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid? TemplateProductId { get; set; }

    public long? CategoryId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string ProductType { get; set; } = null!;

    public decimal BasePrice { get; set; }

    public string Currency { get; set; } = null!;

    public bool IsAvailable { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public string? ImageUrl { get; set; }

    public string? MetadataJson { get; set; }

    public TenantScopeType ScopeType { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public List<ProductVariantResult> Variants { get; set; } = new();
}
