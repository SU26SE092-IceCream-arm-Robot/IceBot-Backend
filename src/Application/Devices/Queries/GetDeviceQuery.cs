using Application.Identity.Tokens.Claims;

namespace Application.Devices.Queries;

public sealed class GetDeviceQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid DeviceId { get; init; }
}
