using Domain.SalesCatalog.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SalesCatalog.Persistence;

internal static class KioskMenuItemQuery
{
    public static IQueryable<MenuItem> Build(
        IceBotDbContext dbContext,
        Guid menuItemId,
        Guid? organizationId,
        Guid storeId,
        Guid kioskId) =>
        dbContext.MenuItems
            .Include(menuItem => menuItem.Menu)
            .Include(menuItem => menuItem.Product)
            .Include(menuItem => menuItem.ProductVariant)
            .Include(menuItem => menuItem.Recipe)
                .ThenInclude(recipe => recipe!.RecipeItems)
                    .ThenInclude(item => item.Ingredient)
            .Where(menuItem =>
                menuItem.Id == menuItemId &&
                menuItem.Product.DeletedAt == null &&
                menuItem.Menu.OrganizationId == organizationId &&
                (!menuItem.Menu.StoreId.HasValue || menuItem.Menu.StoreId == storeId) &&
                (!menuItem.Menu.KioskId.HasValue || menuItem.Menu.KioskId == kioskId) &&
                menuItem.Product.OrganizationId == organizationId &&
                (!menuItem.Product.StoreId.HasValue || menuItem.Product.StoreId == storeId) &&
                (!menuItem.Product.KioskId.HasValue || menuItem.Product.KioskId == kioskId));
}
