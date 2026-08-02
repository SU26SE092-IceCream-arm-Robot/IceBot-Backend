using Application.Devices.Telemetry;
using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.ReadModels;
using Application.SalesCatalog.Rules;
using Application.SalesCatalog.RuntimeMenus.Mapping;
using Application.SalesCatalog.RuntimeMenus.Results;
using Application.SalesCatalog.RuntimeMenus.Support;
using Domain.Catalog.Enums;
using Domain.Tenants.Entities;
using Microsoft.Extensions.Options;

namespace Application.SalesCatalog.RuntimeMenus.Services;

public sealed class RuntimeMenuProjectionBuilder
{
    private readonly IMenuStore _menus;
    private readonly EdgeTelemetryIngestionOptions _options;

    public RuntimeMenuProjectionBuilder(
        IMenuStore menus,
        IOptions<EdgeTelemetryIngestionOptions>? options = null)
    {
        _menus = menus;
        _options = options?.Value ?? new EdgeTelemetryIngestionOptions();
    }

    public async Task<RuntimeMenuProjection> BuildAsync(
        Kiosk kiosk,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var menus = await _menus.ListActiveMenusForKioskAsync(
            kiosk.OrganizationId,
            kiosk.StoreId,
            kiosk.Id,
            now,
            cancellationToken);

        var candidates = menus
            .SelectMany(menu => menu.MenuItems.Select(item => new { Menu = menu, Item = item }))
            .ToList();
        var menuItemIds = candidates.Select(candidate => candidate.Item.Id).Distinct().ToArray();

        var optionRows = await _menus.ListMenuItemProductOptionsAsync(menuItemIds, cancellationToken);
        var optionsByMenuItem = optionRows.ToLookup(option => option.MenuItemId);
        var optionGroupRows = await _menus.ListMenuItemOptionGroupsAsync(menuItemIds, cancellationToken);
        var optionGroupsByMenuItem = optionGroupRows.ToLookup(group => group.MenuItemId);

        var policyKeys = candidates
            .Where(candidate =>
                candidate.Item.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced &&
                candidate.Item.RecipeId.HasValue)
            .Select(candidate => new ActiveProductionRouteOptionPolicyKey(
                candidate.Item.ProductVariantId,
                candidate.Item.RecipeId!.Value))
            .Distinct()
            .ToArray();
        IReadOnlyDictionary<ActiveProductionRouteOptionPolicyKey, ActiveProductionRouteOptionPolicy> routePolicies =
            policyKeys.Length == 0
                ? new Dictionary<ActiveProductionRouteOptionPolicyKey, ActiveProductionRouteOptionPolicy>()
                : await _menus.GetActiveProductionRouteOptionPoliciesAsync(
                    kiosk.Id,
                    policyKeys,
                    now.AddSeconds(-_options.ReadinessTimeoutSeconds),
                    cancellationToken);

        var filteredOptionsByMenuItem = candidates.ToDictionary(candidate => candidate.Item.Id, candidate =>
        {
            var options = optionsByMenuItem[candidate.Item.Id].ToArray();
            return candidate.Item.ProductVariant.FulfillmentType switch
            {
                FulfillmentType.Packaged => options.Where(option =>
                    option.ExecutionImpact == ProductOptionExecutionImpact.CommercialOnly).ToArray(),
                FulfillmentType.Manual => options,
                FulfillmentType.MachineProduced when candidate.Item.RecipeId.HasValue =>
                    FilterMachineProducedOptions(
                        new ActiveProductionRouteOptionPolicyKey(
                            candidate.Item.ProductVariantId,
                            candidate.Item.RecipeId.Value),
                        options),
                _ => []
            };
        });

        var items = candidates
            .Where(entry =>
            {
                var routeKey = entry.Item.RecipeId.HasValue
                    ? new ActiveProductionRouteOptionPolicyKey(entry.Item.ProductVariantId, entry.Item.RecipeId.Value)
                    : (ActiveProductionRouteOptionPolicyKey?)null;
                var hasActiveProductionRoute =
                    entry.Item.ProductVariant.FulfillmentType == FulfillmentType.MachineProduced &&
                    routeKey.HasValue &&
                    routePolicies.ContainsKey(routeKey.Value);
                return MenuItemSellabilityRules.Validate(
                           entry.Item,
                           kiosk,
                           now,
                           hasActiveProductionRoute) is null &&
                       ProductOptionSelectionRules.IsSatisfiable(
                           optionGroupsByMenuItem[entry.Item.Id].ToArray(),
                           filteredOptionsByMenuItem[entry.Item.Id]);
            })
            .OrderBy(entry => entry.Menu.DisplayOrder)
            .ThenBy(entry => entry.Item.DisplayOrder)
            .ThenBy(entry => entry.Item.DisplayName)
            .Select(entry => RuntimeMenuResultMapper.ToResult(entry.Item, filteredOptionsByMenuItem[entry.Item.Id]))
            .ToList();

        return new RuntimeMenuProjection(RuntimeMenuRevision.Compute(kiosk.Id, items), items);

        MenuItemProductOptionReadModel[] FilterMachineProducedOptions(
            ActiveProductionRouteOptionPolicyKey key,
            MenuItemProductOptionReadModel[] sourceOptions)
        {
            return !routePolicies.TryGetValue(key, out var policy)
                ? []
                : sourceOptions.Where(option =>
                    option.ExecutionImpact != ProductOptionExecutionImpact.ProductionAffecting ||
                    policy.SupportedOptionCodes.Contains(option.Code)).ToArray();
        }
    }
}
