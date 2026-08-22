using Domain.Common;

namespace Domain.Orders.Incidents;

public sealed class ProductionIncidentHistory : GuidEntity
{
    public Guid ProductionIncidentId { get; private set; }
    public string Action { get; private set; } = null!;
    public ProductionIncidentStatus? FromStatus { get; private set; }
    public ProductionIncidentStatus ToStatus { get; private set; }
    public Guid? ActorAccountId { get; private set; }
    public string Reason { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public ProductionIncident ProductionIncident { get; private set; } = null!;

    private ProductionIncidentHistory() { }

    internal static ProductionIncidentHistory Create(Guid incidentId, string action,
        ProductionIncidentStatus? from, ProductionIncidentStatus to, Guid? actorId,
        string reason, DateTimeOffset at, Guid? relatedEntityId)
    {
        if (string.IsNullOrWhiteSpace(action) || action.Trim().Length > 100)
            throw new DomainRuleException("Incident history action is required and cannot exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            throw new DomainRuleException("Incident history reason is required and cannot exceed 1000 characters.");
        return new ProductionIncidentHistory
        {
            Id = Guid.NewGuid(),
            ProductionIncidentId = incidentId,
            Action = action.Trim(),
            FromStatus = from,
            ToStatus = to,
            ActorAccountId = actorId,
            Reason = reason.Trim(),
            OccurredAt = at,
            RelatedEntityId = relatedEntityId
        };
    }
}
