using Domain.Common;
using Domain.Tenants.Enums;

namespace Domain.Tenants.Entities;

public sealed class KioskOperationalStateTransition : AuditedEntity
{
    public Guid KioskId { get; set; }
    public KioskOperationalState FromState { get; set; }
    public KioskOperationalState ToState { get; set; }
    public string Reason { get; set; } = null!;
    public DateTimeOffset ChangedAt { get; set; }
    public Guid ChangedByAccountId { get; set; }
    public Guid? SourceMaintenanceTicketId { get; set; }
}
