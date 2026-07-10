using Application.Identity.Tokens.Claims;

namespace Application.Devices.Catalog.Queries;

public sealed class GetDeviceQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public required Guid DeviceId { get; init; }
}
