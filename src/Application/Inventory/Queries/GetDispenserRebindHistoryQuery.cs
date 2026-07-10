using Application.Identity.Tokens.Claims;

namespace Application.Inventory.Queries;

public sealed record GetDispenserRebindHistoryQuery(Guid KioskId, Guid DispenserStateId, CurrentUserContext UserContext);
