using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Stores.Commands;

public sealed class DisableStoreCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid StoreId { get; init; }
}
