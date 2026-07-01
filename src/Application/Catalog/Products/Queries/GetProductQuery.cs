using Application.Identity.Tokens.Claims;

namespace Application.Catalog.Products.Queries;

public sealed class GetProductQuery
{
    public Guid ProductId { get; init; }
    public Guid? OrganizationId { get; init; }
    public bool IsGlobalTemplate { get; init; }
    public required CurrentUserContext UserContext { get; init; }

    public GetProductQuery(Guid productId)
    {
        ProductId = productId;
    }
}
