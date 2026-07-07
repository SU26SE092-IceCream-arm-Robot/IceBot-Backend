namespace Domain.Inventory.Enums;

public enum InventoryReadinessStatus
{
    Ready = 1,
    MissingIngredient = 2,
    ContainerInactive = 3,
    DeviceUnavailable = 4,
    CalibrationMissing = 5
}
