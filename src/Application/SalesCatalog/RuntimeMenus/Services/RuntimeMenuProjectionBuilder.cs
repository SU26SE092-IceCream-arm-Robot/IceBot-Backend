using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.ReadModels;
using Application.SalesCatalog.Rules;
using Application.SalesCatalog.RuntimeMenus.Mapping;
using Application.SalesCatalog.RuntimeMenus.Results;
using Application.SalesCatalog.RuntimeMenus.Support;
using Domain.Catalog.Enums;
using Domain.Tenants.Entities;

namespace Application.SalesCatalog.RuntimeMenus.Services;

public sealed class RuntimeMenuProjectionBuilder(IMenuStore menus)
{
    private readonly IMenuStore _menus = menus;

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

        var filteredOptionsByMenuItem = candidates.ToDictionary(candidate => candidate.Item.Id, candidate =>
        {
            var options = optionsByMenuItem[candidate.Item.Id].ToArray();
            return candidate.Item.ProductVariant.FulfillmentType switch
            {
                FulfillmentType.Packaged => options.Where(option =>
                    option.ExecutionImpact == ProductOptionExecutionImpact.CommercialOnly).ToArray(),
                FulfillmentType.Manual => options,
                // Production-affecting options are filtered by the live route policy at read time.
                FulfillmentType.MachineProduced => options,
                _ => []
            };
        });

        var items = new List<RuntimeMenuItemResult>();
        foreach (var entry in candidates
                     .OrderBy(entry => entry.Menu.DisplayOrder)
                     .ThenBy(entry => entry.Item.DisplayOrder)
                     .ThenBy(entry => entry.Item.DisplayName))
        {
            if (MenuItemSellabilityRules.ValidateStatic(entry.Item, kiosk, now) is not null ||
                !ProductOptionSelectionRules.IsSatisfiable(
                    optionGroupsByMenuItem[entry.Item.Id].ToArray(),
                    filteredOptionsByMenuItem[entry.Item.Id]))
            {
                continue;
            }

            items.Add(RuntimeMenuResultMapper.ToResult(entry.Item, filteredOptionsByMenuItem[entry.Item.Id]));
        }

        return new RuntimeMenuProjection(RuntimeMenuRevision.Compute(kiosk.Id, items), items);
    }
}
