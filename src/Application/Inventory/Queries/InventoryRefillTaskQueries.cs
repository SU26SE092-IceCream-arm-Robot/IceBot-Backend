using Application.Identity.Tokens.Claims;
using Domain.Inventory.Enums;

namespace Application.Inventory.Queries;

public sealed record ListInventoryRefillTasksQuery(
    Guid KioskId,
    InventoryRefillTaskStatus? Status,
    DateTimeOffset? RequestedFrom,
    DateTimeOffset? RequestedTo,
    int PageNumber,
    int PageSize,
    CurrentUserContext UserContext);
public sealed record GetInventoryRefillTaskQuery(Guid KioskId, Guid TaskId, CurrentUserContext UserContext);
