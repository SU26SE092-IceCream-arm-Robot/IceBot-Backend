using Application.Identity.Tokens.Claims;

namespace Application.Identity.CurrentAccount.Queries;

public sealed class GetCurrentAccountAccessQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public required IReadOnlyCollection<string> RoleCodes { get; init; }
    public required IReadOnlyCollection<string> RoleScopeClaims { get; init; }
}
