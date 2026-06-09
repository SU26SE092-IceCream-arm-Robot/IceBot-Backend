using Application.Identity.Tokens.Claims;

namespace Application.Identity.InternalAccounts.Queries;

public sealed class ListInternalAccountsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}

