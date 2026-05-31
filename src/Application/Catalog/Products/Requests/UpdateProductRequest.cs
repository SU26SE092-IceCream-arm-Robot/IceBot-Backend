using Domain.Tenants.Enums;

namespace Application.Catalog.Products.Requests;

public sealed class UpdateProductRequest
{
    public Guid? OrganizationId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid? TemplateProductId { get; set; }

    public long? CategoryId { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string? ProductType { get; set; }

    public decimal? BasePrice { get; set; }

    public string? Currency { get; set; }

    public bool? IsAvailable { get; set; }

    public int? PreparationTimeSeconds { get; set; }

    public string? ImageUrl { get; set; }

    public string? MetadataJson { get; set; }

    public TenantScopeType? ScopeType { get; set; }
}
