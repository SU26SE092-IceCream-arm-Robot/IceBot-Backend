using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Organizations.Queries;

public sealed class ListOrganizationSalesSummariesQuery
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public Guid? OrganizationId { get; init; }
    public string? Search { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
