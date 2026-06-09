using Application.Identity.Tokens.Claims;
using Application.Tenants.Stores.Requests;

namespace Application.Tenants.Stores.Commands;

public sealed class UpdateStoreCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid StoreId { get; init; }
    public UpdateStoreRequest Request { get; init; } = null!;
}
