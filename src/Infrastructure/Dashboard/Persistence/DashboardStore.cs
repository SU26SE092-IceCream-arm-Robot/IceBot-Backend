using Application.Dashboard.Abstractions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dashboard.Persistence;

public sealed class DashboardStore : IDashboardStore
{
    private readonly IceBotDbContext _dbContext;

    public DashboardStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardMetrics> GetDashboardMetricsAsync(
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        var orgsQuery = _dbContext.Organizations.WhereNotDeleted().AsNoTracking();
        var storesQuery = _dbContext.Stores.WhereNotDeleted().AsNoTracking();
        var kiosksQuery = _dbContext.Kiosks.WhereNotDeleted().AsNoTracking();
        var ordersQuery = _dbContext.Orders.WhereNotDeleted().AsNoTracking();
        var dispensersQuery = _dbContext.IngredientDispenserStates.WhereNotDeleted().AsNoTracking();
        var eventsQuery = _dbContext.DeviceEvents.AsNoTracking();

        if (!isSystemAdmin)
        {
            var allowedOrgs = (allowedOrganizationIds ?? Array.Empty<Guid>()).ToArray();
            var allowedStores = (allowedStoreIds ?? Array.Empty<Guid>()).ToArray();
            var allowedKiosks = (allowedKioskIds ?? Array.Empty<Guid>()).ToArray();

            orgsQuery = orgsQuery.Where(o => allowedOrgs.Contains(o.Id));
            storesQuery = storesQuery.Where(s => allowedOrgs.Contains(s.OrganizationId) || allowedStores.Contains(s.Id));
            kiosksQuery = kiosksQuery.Where(k => allowedOrgs.Contains(k.OrganizationId) || allowedStores.Contains(k.StoreId) || allowedKiosks.Contains(k.Id));
            ordersQuery = ordersQuery.Where(o => (o.OrganizationId.HasValue && allowedOrgs.Contains(o.OrganizationId.Value)) || (o.StoreId.HasValue && allowedStores.Contains(o.StoreId.Value)) || allowedKiosks.Contains(o.KioskId));

            dispensersQuery = dispensersQuery.Where(d => d.KioskId.HasValue && kiosksQuery.Select(k => k.Id).Contains(d.KioskId.Value));
            eventsQuery = eventsQuery.Where(e => e.KioskId.HasValue && kiosksQuery.Select(k => k.Id).Contains(e.KioskId.Value));
        }

        var orgCount = await orgsQuery.CountAsync(cancellationToken);
        var storeCount = await storesQuery.CountAsync(cancellationToken);

        var kioskStats = await kiosksQuery
            .GroupBy(k => k.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var kioskCount = kioskStats.Sum(x => x.Count);
        var activeKioskCount = kioskStats.FirstOrDefault(x => x.Status == Domain.Tenants.Enums.KioskStatus.Active)?.Count ?? 0;
        var visibleKioskIds = kiosksQuery.Select(kiosk => kiosk.Id);
        var offlineKioskCount = await _dbContext.KioskConnectivityProjections.AsNoTracking()
            .CountAsync(
                connectivity => visibleKioskIds.Contains(connectivity.KioskId) &&
                    connectivity.Status == Domain.Devices.Connectivity.KioskConnectivityStatus.Unreachable,
                cancellationToken);
        var maintenanceKioskCount = kioskStats.FirstOrDefault(x => x.Status == Domain.Tenants.Enums.KioskStatus.Maintenance)?.Count ?? 0;

        var pendingOrderCount = await ordersQuery.CountAsync(o => o.Status == Domain.Orders.Enums.OrderStatus.PendingPayment, cancellationToken);
        var paidOrderCount = await ordersQuery.CountAsync(o => o.Status == Domain.Orders.Enums.OrderStatus.Paid, cancellationToken);
        var refundRequiredOrderCount = await ordersQuery.CountAsync(o => o.Status == Domain.Orders.Enums.OrderStatus.RefundRequired, cancellationToken);

        var lowStockDispenserCount = await dispensersQuery.CountAsync(d => d.CurrentLevelStatus == Domain.Inventory.Enums.IngredientLevelStatus.Low, cancellationToken);

        var cutoffTime = DateTimeOffset.UtcNow.AddDays(-1);
        var latestDeviceEventCount = await eventsQuery.CountAsync(e => e.OccurredAt >= cutoffTime, cancellationToken);

        return new DashboardMetrics
        {
            OrganizationCount = orgCount,
            StoreCount = storeCount,
            KioskCount = kioskCount,
            ActiveKioskCount = activeKioskCount,
            OfflineKioskCount = offlineKioskCount,
            MaintenanceKioskCount = maintenanceKioskCount,
            PendingOrderCount = pendingOrderCount,
            PaidOrderCount = paidOrderCount,
            RefundRequiredOrderCount = refundRequiredOrderCount,
            LowStockDispenserCount = lowStockDispenserCount,
            LatestDeviceEventCount = latestDeviceEventCount
        };
    }
}
