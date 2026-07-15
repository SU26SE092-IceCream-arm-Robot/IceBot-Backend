namespace Application.Devices.Connectivity.Results;

public class KioskStatusOverviewResult
{
    public int TotalCount { get; set; }
    public List<KioskStatusSummaryDto> ByLifecycleStatus { get; set; } = new();
    public List<KioskStatusSummaryDto> ByConnectivityStatus { get; set; } = new();
    public List<KioskStatusOverviewItemDto> Items { get; set; } = new();
}

public class KioskStatusSummaryDto
{
    public string Status { get; set; } = null!;
    public int Count { get; set; }
}

public class KioskStatusOverviewItemDto
{
    public Guid KioskId { get; set; }
    public string KioskCode { get; set; } = null!;
    public string KioskName { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = null!;
    public string LifecycleStatus { get; set; } = null!;
    public string ConnectivityStatus { get; set; } = null!;
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public string? LastEventSeverity { get; set; }
    public DateTimeOffset? LastEventAt { get; set; }
    public string? ExecutionReadiness { get; set; }
    public string? ExecutionActivity { get; set; }
    public string? ExecutionSafety { get; set; }
    public string? ExecutionFaultCode { get; set; }
    public DateTimeOffset? ReadinessReportedAt { get; set; }
    public int AvailableCapabilityCount { get; set; }
    public int UnavailableCapabilityCount { get; set; }
}
