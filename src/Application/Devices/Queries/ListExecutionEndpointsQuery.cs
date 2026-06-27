using Application.Identity.Tokens.Claims;

namespace Application.Devices.Queries;

public sealed class ListExecutionEndpointsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public string? Profile { get; init; }
    public string? Status { get; init; }
}
