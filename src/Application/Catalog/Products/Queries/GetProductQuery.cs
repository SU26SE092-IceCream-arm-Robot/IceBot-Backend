using Application.Identity.Tokens.Claims;
using System;

namespace Application.Catalog.Products.Queries;

public sealed class GetProductQuery
{
    public Guid ProductId { get; init; }
    public required CurrentUserContext UserContext { get; init; }

    public GetProductQuery(Guid productId)
    {
        ProductId = productId;
    }
}

