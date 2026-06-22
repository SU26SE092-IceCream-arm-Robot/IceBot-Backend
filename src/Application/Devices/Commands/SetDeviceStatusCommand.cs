using Application.Devices.Requests;
using Application.Identity.Tokens.Claims;

namespace Application.Devices.Commands;

public sealed class SetDeviceStatusCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid DeviceId { get; init; }
    public required SetDeviceStatusRequest Request { get; init; }
}
