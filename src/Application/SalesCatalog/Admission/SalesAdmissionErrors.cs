using Application.Shared.Wrappers;

namespace Application.SalesCatalog.Admission;

public static class SalesAdmissionErrors
{
    private static readonly IReadOnlyDictionary<SalesAdmissionBlockerCode, ApiBusinessErrorDefinition> Definitions =
        new Dictionary<SalesAdmissionBlockerCode, ApiBusinessErrorDefinition>
        {
            [SalesAdmissionBlockerCode.OrganizationInactive] = Define("SALES.ORGANIZATION_INACTIVE", "Organization is not active for sales."),
            [SalesAdmissionBlockerCode.StoreInactive] = Define("SALES.STORE_INACTIVE", "Store is not active for sales."),
            [SalesAdmissionBlockerCode.StoreSalesPaused] = Define("SALES.STORE_SALES_PAUSED", "Store is temporarily not accepting new orders."),
            [SalesAdmissionBlockerCode.StoreClosed] = Define("SALES.STORE_CLOSED", "Store is currently closed."),
            [SalesAdmissionBlockerCode.KioskInactive] = Define("SALES.KIOSK_INACTIVE", "Kiosk is not active for sales."),
            [SalesAdmissionBlockerCode.KioskOperationalHold] = Define("SALES.KIOSK_OPERATIONAL_HOLD", "Kiosk is not currently accepting orders."),
            [SalesAdmissionBlockerCode.KioskConnectivityUnavailable] = Define("SALES.KIOSK_CONNECTIVITY_UNAVAILABLE", "Kiosk is not currently reachable for online sales."),
            [SalesAdmissionBlockerCode.CustomerSessionOccupied] = Define("SALES.CUSTOMER_SESSION_OCCUPIED", "Kiosk is currently serving another customer order."),
            [SalesAdmissionBlockerCode.MenuItemPaused] = Define("SALES.MENU_ITEM_PAUSED", "Menu item is paused for this kiosk."),
            [SalesAdmissionBlockerCode.CatalogUnavailable] = Define("SALES.CATALOG_UNAVAILABLE", "Catalog is not currently available for sales."),
            [SalesAdmissionBlockerCode.ProductionRouteUnavailable] = Define("SALES.PRODUCTION_ROUTE_UNAVAILABLE", "Production route is not available for this kiosk."),
            [SalesAdmissionBlockerCode.InventoryMissing] = Define("SALES.INVENTORY_MISSING", "Required inventory is not configured."),
            [SalesAdmissionBlockerCode.InventoryInactive] = Define("SALES.INVENTORY_INACTIVE", "Required inventory is not active."),
            [SalesAdmissionBlockerCode.InventoryDeviceUnavailable] = Define("SALES.INVENTORY_DEVICE_UNAVAILABLE", "Inventory device evidence is unavailable."),
            [SalesAdmissionBlockerCode.InventoryCalibrationMissing] = Define("SALES.INVENTORY_CALIBRATION_MISSING", "Inventory sensor calibration is required."),
            [SalesAdmissionBlockerCode.InventoryExpired] = Define("SALES.INVENTORY_EXPIRED", "Required inventory has expired."),
            [SalesAdmissionBlockerCode.InventoryEvidenceStale] = Define("SALES.INVENTORY_EVIDENCE_STALE", "Inventory evidence is stale."),
            [SalesAdmissionBlockerCode.InventoryUnitMismatch] = Define("SALES.INVENTORY_UNIT_MISMATCH", "Inventory unit does not match the menu item."),
            [SalesAdmissionBlockerCode.InventoryQuantityUnavailable] = Define("SALES.INVENTORY_QUANTITY_UNAVAILABLE", "Required inventory quantity is unavailable."),
            [SalesAdmissionBlockerCode.InventoryInsufficient] = Define("SALES.INVENTORY_INSUFFICIENT", "Current inventory evidence does not support production.")
        };

    public static IReadOnlyList<ApiBusinessErrorDefinition> All { get; } = Definitions.Values.ToArray();

    public static ApiBusinessErrorDefinition For(SalesAdmissionBlockerCode blockerCode) =>
        Definitions.TryGetValue(blockerCode, out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(nameof(blockerCode), blockerCode, "Unsupported sales admission blocker.");

    public static SalesAdmissionBlocker? SelectPrimary(IReadOnlyList<SalesAdmissionBlocker> blockers) =>
        blockers.OrderBy(blocker => GetPriority(blocker.Code)).ThenBy(blocker => blocker.Code).FirstOrDefault();

    private static ApiBusinessErrorDefinition Define(string code, string message) => new(code, 409, message);

    private static int GetPriority(SalesAdmissionBlockerCode blockerCode) => blockerCode switch
    {
        SalesAdmissionBlockerCode.OrganizationInactive => 10,
        SalesAdmissionBlockerCode.StoreInactive => 20,
        SalesAdmissionBlockerCode.KioskInactive => 30,
        SalesAdmissionBlockerCode.KioskOperationalHold => 40,
        SalesAdmissionBlockerCode.StoreSalesPaused => 50,
        SalesAdmissionBlockerCode.StoreClosed => 60,
        SalesAdmissionBlockerCode.KioskConnectivityUnavailable => 70,
        SalesAdmissionBlockerCode.CustomerSessionOccupied => 80,
        SalesAdmissionBlockerCode.MenuItemPaused => 90,
        SalesAdmissionBlockerCode.CatalogUnavailable => 100,
        SalesAdmissionBlockerCode.ProductionRouteUnavailable => 110,
        SalesAdmissionBlockerCode.InventoryMissing => 120,
        SalesAdmissionBlockerCode.InventoryInactive => 130,
        SalesAdmissionBlockerCode.InventoryDeviceUnavailable => 140,
        SalesAdmissionBlockerCode.InventoryCalibrationMissing => 150,
        SalesAdmissionBlockerCode.InventoryExpired => 160,
        SalesAdmissionBlockerCode.InventoryEvidenceStale => 170,
        SalesAdmissionBlockerCode.InventoryUnitMismatch => 180,
        SalesAdmissionBlockerCode.InventoryQuantityUnavailable => 190,
        SalesAdmissionBlockerCode.InventoryInsufficient => 200,
        _ => throw new ArgumentOutOfRangeException(nameof(blockerCode), blockerCode, "Unsupported sales admission blocker.")
    };
}
