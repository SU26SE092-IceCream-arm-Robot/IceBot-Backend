using Domain.Common.Enums;

namespace Application.Devices.Results;

public sealed class DeviceEventResult
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public Guid? KioskId { get; set; }
    public string EventType { get; set; } = null!;
    public SeverityLevel Severity { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
