using Application.Identity.Tokens.Claims;
using Domain.Inventory.Enums;

namespace Application.Inventory.Commands;

public sealed record RequestInventoryRefillTaskCommand(Guid KioskId, Guid InventoryId, decimal? RequestedQuantity, Guid? IngredientDispenserStateId, string? ReasonCode, string? Notes, string IdempotencyKey, CurrentUserContext UserContext, InventoryRefillRequestSource RequestSource = InventoryRefillRequestSource.Manual);
public sealed record StartInventoryRefillTaskCommand(Guid KioskId, Guid TaskId, string IdempotencyKey, CurrentUserContext UserContext);
public sealed record CompleteInventoryRefillTaskCommand(Guid KioskId, Guid TaskId, decimal ActualQuantity, Guid? IngredientDispenserStateId, string? ReasonCode, string? Notes, string? ExternalLotReference, string IdempotencyKey, CurrentUserContext UserContext);
public sealed record CancelInventoryRefillTaskCommand(Guid KioskId, Guid TaskId, string Reason, string IdempotencyKey, CurrentUserContext UserContext);
