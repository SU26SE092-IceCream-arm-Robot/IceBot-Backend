namespace Application.SalesCatalog.Admission;

public enum SalesAdmissionBlockerCode
{
    OrganizationInactive,
    StoreInactive,
    StoreSalesPaused,
    StoreClosed,
    KioskInactive,
    KioskOperationalHold,
    KioskConnectivityUnavailable,
    CustomerSessionOccupied,
    MenuItemPaused,
    CatalogUnavailable,
    ProductionRouteUnavailable,
    InventoryMissing,
    InventoryInactive,
    InventoryDeviceUnavailable,
    InventoryCalibrationMissing,
    InventoryExpired,
    InventoryEvidenceStale,
    InventoryUnitMismatch,
    InventoryQuantityUnavailable,
    InventoryInsufficient
}

public enum SalesAdmissionBlockerScope
{
    Organization,
    Store,
    Kiosk,
    MenuItem,
    ProductionRoute,
    Inventory,
    Payment
}

public sealed record SalesAdmissionBlocker(
    SalesAdmissionBlockerCode Code,
    SalesAdmissionBlockerScope Scope,
    Guid? ResourceId = null,
    string? ResourceCode = null);

public sealed record SalesAdmissionWarning(string Code);

public sealed record KioskSalesAdmissionDecision(
    bool CanExposeCatalog,
    bool CanPlaceOrder,
    bool CanOpenPayment,
    IReadOnlyList<SalesAdmissionBlocker> Blockers,
    IReadOnlyList<SalesAdmissionWarning> Warnings,
    DateTimeOffset EvaluatedAt,
    DateTimeOffset? EvidenceValidUntil)
{
    public SalesAdmissionBlocker? PrimaryBlocker => SalesAdmissionErrors.SelectPrimary(Blockers);

    public string? ToDisplayMessage() => PrimaryBlocker?.Code switch
    {
        SalesAdmissionBlockerCode.OrganizationInactive => "Organization is not active for sales.",
        SalesAdmissionBlockerCode.StoreInactive => "Store is not active for sales.",
        SalesAdmissionBlockerCode.StoreSalesPaused => "Store is temporarily not accepting new orders.",
        SalesAdmissionBlockerCode.StoreClosed => "Store is currently closed.",
        SalesAdmissionBlockerCode.KioskInactive => "Kiosk is not active for sales.",
        SalesAdmissionBlockerCode.KioskOperationalHold => "Kiosk is not currently accepting orders.",
        SalesAdmissionBlockerCode.KioskConnectivityUnavailable => "Kiosk is not currently reachable for online sales.",
        SalesAdmissionBlockerCode.CustomerSessionOccupied => "Kiosk is currently serving another customer order.",
        _ => "Kiosk is not currently available for sales."
    };
}

public sealed record MenuItemOperationalDecision(
    Guid MenuItemId,
    bool CanSell,
    IReadOnlyList<SalesAdmissionBlocker> Blockers,
    IReadOnlyList<SalesAdmissionWarning> Warnings,
    IReadOnlySet<string> SupportedProductionOptionCodes)
{
    public SalesAdmissionBlocker? PrimaryBlocker => SalesAdmissionErrors.SelectPrimary(Blockers);

    public string? ToDisplayMessage(string displayName) => PrimaryBlocker?.Code switch
    {
        SalesAdmissionBlockerCode.MenuItemPaused => $"Menu item '{displayName}' is paused for this kiosk.",
        SalesAdmissionBlockerCode.ProductionRouteUnavailable =>
            $"Menu item '{displayName}' does not have an active production route for this kiosk.",
        SalesAdmissionBlockerCode.InventoryInsufficient =>
            "Current inventory evidence does not support producing this menu item.",
        SalesAdmissionBlockerCode.InventoryMissing =>
            "Required inventory is not configured for this menu item.",
        SalesAdmissionBlockerCode.InventoryInactive =>
            "Required inventory is not active for this menu item.",
        SalesAdmissionBlockerCode.InventoryDeviceUnavailable =>
            "Inventory device evidence is currently unavailable.",
        SalesAdmissionBlockerCode.InventoryCalibrationMissing =>
            "Inventory sensor calibration is required before this menu item can be sold.",
        SalesAdmissionBlockerCode.InventoryExpired =>
            "Required inventory has expired.",
        SalesAdmissionBlockerCode.InventoryEvidenceStale =>
            "Inventory evidence is stale.",
        SalesAdmissionBlockerCode.InventoryUnitMismatch =>
            "Inventory unit does not match this menu item.",
        SalesAdmissionBlockerCode.InventoryQuantityUnavailable =>
            "Required inventory quantity is unavailable.",
        _ => $"Menu item '{displayName}' is not available."
    };
}

public sealed record KioskSalesAdmissionRequest(
    DateTimeOffset EvaluatedAt,
    Guid? ExcludingOrderId = null,
    bool CheckCustomerSession = true);
