using Application.Devices.Telemetry;
using Application.Inventory.Abstractions;
using Application.SalesCatalog.ReadModels;
using Domain.Catalog.Enums;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Microsoft.Extensions.Options;

namespace Application.SalesCatalog.Availability;

/// <summary>
/// Applies inventory evidence to a machine-produced sale. It deliberately does
/// not interpret Lua or technical-contract metadata.
/// </summary>
public sealed class MachineProductionInventoryGate(
    IInventoryReadinessEvaluator inventory,
    IOptions<EdgeTelemetryIngestionOptions> telemetryOptions)
{
    private readonly EdgeTelemetryIngestionOptions _telemetryOptions = telemetryOptions.Value;

    public async Task<MachineProductionInventoryGateResult> EvaluateAsync(
        Kiosk kiosk,
        MenuItem menuItem,
        ActiveProductionRouteOptionPolicy? routePolicy,
        int quantity,
        IReadOnlyCollection<InventoryIngredientRequirementInput>? selectedOptionIngredients,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        if (menuItem.ProductVariant.FulfillmentType != FulfillmentType.MachineProduced)
        {
            return MachineProductionInventoryGateResult.Sellable;
        }

        if (menuItem.Recipe is null || routePolicy is null)
        {
            return new(false, "No active production route is available for this menu item.");
        }

        var result = await inventory.EvaluateKioskAsync(
            kiosk.Id,
            [new InventoryReadinessRouteInput(
                routePolicy.ExecutionRouteId,
                menuItem.Code,
                menuItem.ProductId,
                menuItem.Recipe.Id,
                routePolicy.SupportedOptionCodes,
                menuItem.Product.OrganizationId,
                menuItem.Product.StoreId,
                menuItem.Product.KioskId,
                menuItem.Recipe.OrganizationId,
                menuItem.Recipe.StoreId,
                menuItem.Recipe.KioskId,
                quantity,
                selectedOptionIngredients)],
            cancellationToken,
            new InventoryReadinessEvaluationOptions(
                InventoryReadinessEvaluationPurpose.RuntimeSellability,
                observedAt,
                TimeSpan.FromSeconds(_telemetryOptions.ReadinessTimeoutSeconds)));

        return result is { IsReady: true }
            ? MachineProductionInventoryGateResult.Sellable
            : new(false, "Current inventory evidence does not support producing this menu item.");
    }
}

public sealed record MachineProductionInventoryGateResult(bool CanSell, string? Reason)
{
    public static readonly MachineProductionInventoryGateResult Sellable = new(true, null);
}
