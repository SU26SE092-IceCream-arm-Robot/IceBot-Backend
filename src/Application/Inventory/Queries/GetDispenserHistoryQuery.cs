using Application.Identity.Tokens.Claims;

namespace Application.Inventory.Queries;

public sealed record GetDispenserHistoryQuery(
    Guid DispenserStateId,
    int PageNumber,
    int PageSize,
    CurrentUserContext UserContext);
