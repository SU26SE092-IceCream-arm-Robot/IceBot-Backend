namespace Domain.Inventory.Enums;

/// <summary>
/// Determines which inventory evidence is required for a dispenser state.
/// Sensor observations never change this setting implicitly.
/// </summary>
public enum InventoryTrackingMode
{
    ManualEstimate = 0,
    SensorAssisted = 1,
    SensorRequired = 2
}
