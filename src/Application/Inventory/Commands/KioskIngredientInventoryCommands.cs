using Application.Identity.Tokens.Claims;
using Domain.Inventory.Enums;

namespace Application.Inventory.Commands;

public sealed record CreateKioskIngredientInventoryCommand(
    Guid KioskId,
    Guid IngredientId,
    string Unit,
    decimal? EstimatedQuantity,
    decimal? LowStockThreshold,
    DateTimeOffset? ExpiresAt,
    InventoryTrackingMode TrackingMode,
    CurrentUserContext UserContext);

public sealed record AdjustKioskIngredientInventoryCommand(
    Guid KioskId,
    Guid InventoryId,
    decimal EstimatedQuantity,
    string? ReasonCode,
    CurrentUserContext UserContext);

public sealed record UpdateKioskIngredientInventoryCommand(
    Guid KioskId,
    Guid InventoryId,
    decimal? LowStockThreshold,
    DateTimeOffset? ExpiresAt,
    InventoryTrackingMode TrackingMode,
    CurrentUserContext UserContext);
