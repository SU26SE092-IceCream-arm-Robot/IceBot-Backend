using Application.Orders.Admission;
using Application.SalesCatalog.Admission.Abstractions;
using Application.SalesCatalog.ReadModels;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SalesCatalog.Persistence;

/// <summary>Reads the current facts used by the operational admission policy.</summary>
public sealed class OperationalAdmissionReadStore(IceBotDbContext dbContext) : IOperationalAdmissionReadStore
{
    public Task<Kiosk?> GetKioskAsync(Guid kioskId, CancellationToken cancellationToken = default) =>
        dbContext.Kiosks.WhereNotDeleted().AsNoTracking()
            .Include(kiosk => kiosk.Store)
            .Include(kiosk => kiosk.Organization)
            .FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);

    public Task<Domain.Devices.Connectivity.KioskConnectivityProjection?> GetKioskConnectivityAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        dbContext.KioskConnectivityProjections.AsNoTracking()
            .FirstOrDefaultAsync(value => value.KioskId == kioskId, cancellationToken);

    public Task<bool> HasActiveCustomerSessionAsync(
        Guid kioskId,
        DateTimeOffset observedAt,
        Guid? excludingOrderId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Orders.WhereNotDeleted().AnyAsync(
            KioskCustomerSessionAdmission.BuildActiveSessionPredicate(kioskId, observedAt, excludingOrderId),
            cancellationToken);

    public Task<MenuItem?> GetMenuItemForKioskAsync(
        Guid menuItemId,
        Guid? organizationId,
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        dbContext.MenuItems.AsNoTracking()
            .Include(menuItem => menuItem.Menu)
            .Include(menuItem => menuItem.Product)
            .Include(menuItem => menuItem.ProductVariant)
            .Include(menuItem => menuItem.Recipe)
                .ThenInclude(recipe => recipe!.RecipeItems)
                    .ThenInclude(item => item.Ingredient)
            .FirstOrDefaultAsync(menuItem =>
                menuItem.Id == menuItemId &&
                menuItem.Product.DeletedAt == null &&
                menuItem.Menu.OrganizationId == organizationId &&
                (!menuItem.Menu.StoreId.HasValue || menuItem.Menu.StoreId == storeId) &&
                (!menuItem.Menu.KioskId.HasValue || menuItem.Menu.KioskId == kioskId) &&
                menuItem.Product.OrganizationId == organizationId &&
                (!menuItem.Product.StoreId.HasValue || menuItem.Product.StoreId == storeId) &&
                (!menuItem.Product.KioskId.HasValue || menuItem.Product.KioskId == kioskId),
                cancellationToken);

    public Task<bool> IsMenuItemPausedAsync(Guid kioskId, Guid menuItemId, CancellationToken cancellationToken = default) =>
        dbContext.KioskMenuItemAvailabilities.AsNoTracking().AnyAsync(
            value => value.KioskId == kioskId && value.MenuItemId == menuItemId &&
                value.State == Domain.SalesCatalog.Enums.MenuItemOperationalAvailabilityState.Paused,
            cancellationToken);

    public async Task<ActiveProductionRouteOptionPolicy?> GetActiveProductionRouteOptionPolicyAsync(
        Guid kioskId,
        Guid productVariantId,
        Guid recipeId,
        DateTimeOffset readinessReceivedAfter,
        CancellationToken cancellationToken = default)
    {
        var candidates = await ExecutionRouteAvailabilityReader.ListAsync(dbContext, kioskId,
            [new ActiveProductionRouteOptionPolicyKey(productVariantId, recipeId)], readinessReceivedAfter,
            cancellationToken);
        var route = candidates.FirstOrDefault();
        return route is null
            ? null
            : new ActiveProductionRouteOptionPolicy(route.Id, route.SupportedOptionCodes);
    }
}
