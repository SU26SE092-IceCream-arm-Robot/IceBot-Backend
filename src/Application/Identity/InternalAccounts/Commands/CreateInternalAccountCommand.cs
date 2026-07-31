using Application.Identity.InternalAccounts.Requests;
using Application.Identity.Tokens.Claims;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class CreateInternalAccountCommand
{
    public CreateInternalAccountRequest Request { get; init; } = null!;
    public Guid OrganizationId { get; init; }
    public Guid? CreatedByAccountId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required IReadOnlyCollection<string> UserRoles { get; init; }
}
