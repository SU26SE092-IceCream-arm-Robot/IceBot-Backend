using Application.Identity.Tokens.Claims;

namespace Application.Inventory.Queries;

public sealed record GetDispenserHistoryQuery(
    Guid KioskId,
    Guid DispenserStateId,
    int PageNumber,
    int PageSize,
    CurrentUserContext UserContext);
