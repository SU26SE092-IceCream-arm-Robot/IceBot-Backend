using Application.Devices.Catalog.Requests;
using Application.Identity.Tokens.Claims;

namespace Application.Devices.Catalog.Commands;

public sealed record ReplaceDeviceCommand(
    Guid KioskId,
    Guid SourceDeviceId,
    ReplaceDeviceRequest Request,
    CurrentUserContext UserContext);
