using Application.Identity.Tokens.Claims;

namespace Application.Identity.InternalAccounts.Queries;

public sealed class GetInternalAccountQuery
{
    public Guid AccountId { get; init; }
    public Guid OrganizationId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}

