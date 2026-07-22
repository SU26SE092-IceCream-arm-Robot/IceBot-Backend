using Application.Devices.Catalog.Requests;
using Application.Identity.Tokens.Claims;

namespace Application.Devices.Catalog.Commands;

public sealed class UpdateDeviceCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public required Guid DeviceId { get; init; }
    public required UpdateDeviceRequest Request { get; init; }
}
