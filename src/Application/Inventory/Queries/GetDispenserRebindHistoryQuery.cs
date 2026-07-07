using Application.Identity.Tokens.Claims;

namespace Application.Inventory.Queries;

public sealed record GetDispenserRebindHistoryQuery(Guid DispenserStateId, CurrentUserContext UserContext);
