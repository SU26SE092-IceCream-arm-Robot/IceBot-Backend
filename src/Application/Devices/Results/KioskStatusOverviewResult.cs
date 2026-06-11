namespace Application.Devices.Results;

public class KioskStatusOverviewResult
{
    public int TotalCount { get; set; }
    public List<KioskStatusSummaryDto> ByStatus { get; set; } = new();
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
    public string Status { get; set; } = null!;
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public string? LastEventSeverity { get; set; }
    public DateTimeOffset? LastEventAt { get; set; }
}
