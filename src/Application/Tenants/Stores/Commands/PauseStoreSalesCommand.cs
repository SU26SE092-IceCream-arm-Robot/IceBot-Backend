using Application.Identity.Tokens.Claims;
using Application.Tenants.Stores.Requests;

namespace Application.Tenants.Stores.Commands;

public sealed class PauseStoreSalesCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid OrganizationId { get; init; }
    public Guid StoreId { get; init; }
    public PauseStoreSalesRequest Request { get; init; } = null!;
}
