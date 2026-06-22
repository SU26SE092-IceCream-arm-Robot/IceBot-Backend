using Application.Devices.Requests;
using Application.Identity.Tokens.Claims;

namespace Application.Devices.Commands;

public sealed class CreateDeviceCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public required CreateDeviceRequest Request { get; init; }
}
