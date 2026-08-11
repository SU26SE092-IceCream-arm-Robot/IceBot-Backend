using Application.Identity.Tokens.Claims;

namespace Application.Identity.InternalAccounts.Queries;

public sealed class GetInternalAccountEffectiveAccessQuery
{
    public required Guid AccountId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}
