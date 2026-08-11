using Application.Identity.InternalAccounts.Requests;
using Application.Identity.Tokens.Claims;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class UpdateInternalAccountCommand
{
    public Guid AccountId { get; init; }
    public Guid OrganizationId { get; init; }
    public UpdateInternalAccountRequest Request { get; init; } = null!;
    public Guid? UpdatedByAccountId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}
