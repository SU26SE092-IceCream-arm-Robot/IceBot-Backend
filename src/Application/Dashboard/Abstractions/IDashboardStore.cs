namespace Application.Dashboard.Abstractions;

public class DashboardMetrics
{
    public int OrganizationCount { get; set; }
    public int StoreCount { get; set; }
    public int KioskCount { get; set; }
    public int ActiveKioskCount { get; set; }
    public int OfflineKioskCount { get; set; }
    public int MaintenanceKioskCount { get; set; }
    public int PendingOrderCount { get; set; }
    public int PaidOrderCount { get; set; }
    public int RefundRequiredOrderCount { get; set; }
    public int LowStockDispenserCount { get; set; }
    public int LatestDeviceEventCount { get; set; }
}

public interface IDashboardStore
{
    Task<DashboardMetrics> GetDashboardMetricsAsync(
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);
}
