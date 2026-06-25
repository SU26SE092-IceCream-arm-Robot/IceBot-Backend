using Application.Devices.Requests;
using Application.Identity.Tokens.Claims;

namespace Application.Devices.Commands;

public sealed class UpdateDeviceCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid DeviceId { get; init; }
    public required UpdateDeviceRequest Request { get; init; }
}
