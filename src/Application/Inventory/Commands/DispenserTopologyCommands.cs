using Application.Identity.Tokens.Claims;
using Application.Inventory.Requests;

namespace Application.Inventory.Commands;

public sealed record CreateDispenserStateCommand(
    Guid KioskId,
    CreateDispenserStateRequest Request,
    CurrentUserContext UserContext);

public sealed record UpdateDispenserStateCommand(
    Guid KioskId,
    Guid DispenserStateId,
    UpdateDispenserStateRequest Request,
    CurrentUserContext UserContext);

public sealed record SetDispenserStateStatusCommand(
    Guid KioskId,
    Guid DispenserStateId,
    bool IsActive,
    string Reason,
    CurrentUserContext UserContext);

public sealed record DeleteDispenserStateCommand(
    Guid KioskId,
    Guid DispenserStateId,
    CurrentUserContext UserContext);

public sealed record RebindDispenserStateCommand(
    Guid KioskId,
    Guid DispenserStateId,
    RebindDispenserStateRequest Request,
    CurrentUserContext UserContext);
