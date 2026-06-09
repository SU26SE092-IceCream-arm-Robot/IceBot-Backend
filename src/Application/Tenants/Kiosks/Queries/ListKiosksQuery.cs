using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Kiosks.Queries;

public sealed class ListKiosksQuery
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
}
