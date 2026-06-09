using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Stores.Queries;

public sealed class GetStoreQuery
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid StoreId { get; init; }
}
