using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public partial class RobotJobEvent : RobotRuntimeEventEntity
{
    public Guid RobotJobId { get; set; }

    public Guid? RobotJobStepId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid EventId { get; set; }

    public Guid? CorrelationId { get; set; }

    public Guid? CausationId { get; set; }

    public string EventType { get; set; } = null!;

    public SeverityLevel Severity { get; set; } = SeverityLevel.Info;

    public string? Message { get; set; }

    public string? PayloadJson { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public virtual Device? Device { get; set; }

    public virtual RobotJob RobotJob { get; set; } = null!;

    public virtual RobotJobStep? RobotJobStep { get; set; }
}
