using Domain.Common;
using Domain.Enums;
using Domain.Identity.Entities;

namespace Domain.Entities;

public partial class OperationLog : AppendOnlySyncEntity
{
    public Guid? AccountId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? RobotJobId { get; set; }

    public Guid? SourceEventId { get; set; }

    public Guid? CorrelationId { get; set; }

    public Guid? CausationId { get; set; }

    public string Action { get; set; } = null!;

    public string Category { get; set; } = "System";

    public SeverityLevel Severity { get; set; } = SeverityLevel.Info;

    public string? Message { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? PayloadJson { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public virtual Account? Account { get; set; }

    public virtual Device? Device { get; set; }

    public virtual Kiosk? Kiosk { get; set; }

    public virtual Order? Order { get; set; }

    public virtual RobotJob? RobotJob { get; set; }
}
