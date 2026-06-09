using Application.Identity.Tokens.Claims;
using Application.Tenants.Stores.Requests;

namespace Application.Tenants.Stores.Commands;

public sealed class CreateStoreCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid OrganizationId { get; init; }
    public CreateStoreRequest Request { get; init; } = null!;
}
