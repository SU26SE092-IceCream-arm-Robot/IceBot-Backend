using Application.Devices.Telemetry;
using Application.Inventory.Abstractions;
using Application.SalesCatalog.Admission.Abstractions;
using Application.SalesCatalog.Availability;
using Application.SalesCatalog.Rules;
using Domain.Catalog.Enums;
using Domain.Inventory.Enums;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Microsoft.Extensions.Options;

namespace Application.SalesCatalog.Admission.Services;

public sealed class MenuItemOperationalAdmissionEvaluator(
    IOperationalAdmissionReadStore readStore,
    MachineProductionInventoryGate inventoryGate,
    IOptions<EdgeTelemetryIngestionOptions> telemetryOptions) : IMenuItemOperationalAdmissionEvaluator
{
    private static readonly IReadOnlySet<string> EmptySupportedOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly EdgeTelemetryIngestionOptions _telemetry = telemetryOptions.Value;

    public async Task<MenuItemOperationalDecision> EvaluateAsync(
        Kiosk kiosk,
        Guid menuItemId,
        int quantity,
        IReadOnlyCollection<InventoryIngredientRequirementInput>? selectedOptionIngredients,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        var menuItem = await readStore.GetMenuItemForKioskAsync(
            menuItemId,
            kiosk.OrganizationId,
            kiosk.StoreId,
            kiosk.Id,
            cancellationToken);
        if (menuItem is null)
        {
            return Blocked(menuItemId, SalesAdmissionBlockerCode.CatalogUnavailable,
                SalesAdmissionBlockerScope.MenuItem, menuItemId);
        }

        if (await readStore.IsMenuItemPausedAsync(kiosk.Id, menuItemId, cancellationToken))
        {
            return Blocked(menuItemId, SalesAdmissionBlockerCode.MenuItemPaused,
                SalesAdmissionBlockerScope.MenuItem, menuItemId, menuItem.Code);
        }

        var routePolicy = menuItem.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced && menuItem.Recipe is not null
            ? await readStore.GetActiveProductionRouteOptionPolicyAsync(
                kiosk.Id,
                menuItem.ProductVariantId,
                menuItem.Recipe.Id,
                observedAt.AddSeconds(-_telemetry.ReadinessTimeoutSeconds),
                cancellationToken)
            : null;
        var sellabilityError = MenuItemSellabilityRules.Validate(menuItem, kiosk, observedAt, routePolicy is not null);
        if (sellabilityError is not null)
        {
            var routeMissing = menuItem.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced && routePolicy is null;
            return Blocked(
                menuItemId,
                routeMissing ? SalesAdmissionBlockerCode.ProductionRouteUnavailable : SalesAdmissionBlockerCode.CatalogUnavailable,
                routeMissing ? SalesAdmissionBlockerScope.ProductionRoute : SalesAdmissionBlockerScope.MenuItem,
                routePolicy?.ExecutionRouteId ?? menuItemId,
                routeMissing ? null : menuItem.Code,
                routePolicy?.SupportedOptionCodes ?? EmptySupportedOptions);
        }

        var inventory = await inventoryGate.EvaluateAsync(
            kiosk,
            menuItem,
            routePolicy,
            quantity,
            selectedOptionIngredients,
            observedAt,
            cancellationToken);
        if (!inventory.CanSell)
        {
            return Blocked(menuItemId, ToInventoryBlockerCode(inventory.Status),
                SalesAdmissionBlockerScope.Inventory, null, null,
                routePolicy?.SupportedOptionCodes ?? EmptySupportedOptions);
        }

        return new MenuItemOperationalDecision(
            menuItemId,
            true,
            [],
            [],
            routePolicy?.SupportedOptionCodes ?? EmptySupportedOptions);
    }

    private static MenuItemOperationalDecision Blocked(
        Guid menuItemId,
        SalesAdmissionBlockerCode code,
        SalesAdmissionBlockerScope scope,
        Guid? resourceId,
        string? resourceCode = null,
        IReadOnlySet<string>? supportedProductionOptionCodes = null) =>
        new(
            menuItemId,
            false,
            [new SalesAdmissionBlocker(code, scope, resourceId, resourceCode)],
            [],
            supportedProductionOptionCodes ?? EmptySupportedOptions);

    private static SalesAdmissionBlockerCode ToInventoryBlockerCode(InventoryReadinessStatus? status) => status switch
    {
        InventoryReadinessStatus.MissingIngredient => SalesAdmissionBlockerCode.InventoryMissing,
        InventoryReadinessStatus.ContainerInactive => SalesAdmissionBlockerCode.InventoryInactive,
        InventoryReadinessStatus.DeviceUnavailable => SalesAdmissionBlockerCode.InventoryDeviceUnavailable,
        InventoryReadinessStatus.CalibrationMissing => SalesAdmissionBlockerCode.InventoryCalibrationMissing,
        InventoryReadinessStatus.IngredientExpired => SalesAdmissionBlockerCode.InventoryExpired,
        InventoryReadinessStatus.InventoryEvidenceStale => SalesAdmissionBlockerCode.InventoryEvidenceStale,
        InventoryReadinessStatus.UnitMismatch => SalesAdmissionBlockerCode.InventoryUnitMismatch,
        InventoryReadinessStatus.QuantityUnavailable => SalesAdmissionBlockerCode.InventoryQuantityUnavailable,
        _ => SalesAdmissionBlockerCode.InventoryInsufficient
    };
}
