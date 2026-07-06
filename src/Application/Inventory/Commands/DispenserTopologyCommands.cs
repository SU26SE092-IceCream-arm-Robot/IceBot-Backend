using Application.Identity.Tokens.Claims;
using Application.Inventory.Requests;

namespace Application.Inventory.Commands;

public sealed record CreateDispenserStateCommand(
    Guid KioskId,
    CreateDispenserStateRequest Request,
    CurrentUserContext UserContext);

public sealed record UpdateDispenserStateCommand(
    Guid DispenserStateId,
    UpdateDispenserStateRequest Request,
    CurrentUserContext UserContext);

public sealed record SetDispenserStateStatusCommand(
    Guid DispenserStateId,
    bool IsActive,
    CurrentUserContext UserContext);

public sealed record DeleteDispenserStateCommand(
    Guid DispenserStateId,
    CurrentUserContext UserContext);
