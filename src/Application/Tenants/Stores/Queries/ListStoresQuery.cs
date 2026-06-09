using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Stores.Queries;

public sealed class ListStoresQuery
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid? OrganizationId { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
}
