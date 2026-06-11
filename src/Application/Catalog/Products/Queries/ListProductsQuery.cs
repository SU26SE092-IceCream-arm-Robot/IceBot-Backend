using Application.Identity.Tokens.Claims;

namespace Application.Catalog.Products.Queries;

public sealed class ListProductsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public string? Search { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}

