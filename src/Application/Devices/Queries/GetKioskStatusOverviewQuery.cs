using Application.Identity.Tokens.Claims;

namespace Application.Devices.Queries;

public sealed class GetKioskStatusOverviewQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
}
