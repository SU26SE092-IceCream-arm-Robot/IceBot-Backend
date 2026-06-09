using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Organizations.Queries;

public sealed class ListOrganizationsQuery
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}
