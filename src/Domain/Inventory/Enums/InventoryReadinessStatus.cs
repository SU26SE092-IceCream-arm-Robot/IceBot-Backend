namespace Domain.Inventory.Enums;

public enum InventoryReadinessStatus
{
    Ready = 1,
    MissingIngredient = 2,
    ContainerInactive = 3,
    DeviceUnavailable = 4,
    CalibrationMissing = 5,
    InventoryEvidenceStale = 6,
    IngredientExpired = 7,
    QuantityUnavailable = 8,
    QuantityInsufficient = 9,
    UnitMismatch = 10
}
