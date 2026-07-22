namespace Application.Inventory.Support;

public static class InventoryConcurrency
{
    public static string DeviceTopologyLockKey(Guid deviceId) =>
        $"inventory-device-topology:{deviceId:N}";

    public static string DispenserLockKey(Guid dispenserStateId) =>
        $"inventory-dispenser:{dispenserStateId:N}";
}
