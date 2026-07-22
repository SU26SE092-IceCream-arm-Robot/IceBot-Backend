using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Stores.Commands;

public sealed class ResumeStoreSalesCommand
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid OrganizationId { get; init; }
    public Guid StoreId { get; init; }
}
