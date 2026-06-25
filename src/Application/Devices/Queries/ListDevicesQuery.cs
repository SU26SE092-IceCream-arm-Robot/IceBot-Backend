using Application.Identity.Tokens.Claims;

namespace Application.Devices.Queries;

public sealed class ListDevicesQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
}
