using Application.Identity.Tokens.Claims;

namespace Application.Devices.Commands;

public sealed class RetireDeviceCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid DeviceId { get; init; }
    public string? Reason { get; init; }
}
