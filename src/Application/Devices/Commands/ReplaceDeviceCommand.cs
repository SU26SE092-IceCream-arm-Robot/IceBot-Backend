using Application.Devices.Requests;
using Application.Identity.Tokens.Claims;

namespace Application.Devices.Commands;

public sealed record ReplaceDeviceCommand(
    Guid SourceDeviceId,
    ReplaceDeviceRequest Request,
    CurrentUserContext UserContext);
