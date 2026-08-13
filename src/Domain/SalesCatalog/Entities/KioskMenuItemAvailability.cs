using Domain.Common;
using Domain.SalesCatalog.Enums;

namespace Domain.SalesCatalog.Entities;

public sealed class KioskMenuItemAvailability : GuidEntity, IAuditable, IOrganizationScoped
{
    public Guid? OrganizationId { get; set; }
    public Guid StoreId { get; set; }
    public Guid KioskId { get; set; }
    public Guid MenuId { get; set; }
    public Guid MenuItemId { get; set; }
    public MenuItemOperationalAvailabilityState State { get; set; } = MenuItemOperationalAvailabilityState.Available;
    public MenuItemOperationalAvailabilityReasonCode ReasonCode { get; set; }
    public string Reason { get; set; } = null!;
    public long Revision { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public Guid ChangedByAccountId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedByAccountId { get; set; }
    public Guid? UpdatedByAccountId { get; set; }
    public List<KioskMenuItemAvailabilityTransition> Transitions { get; set; } = [];

    public KioskMenuItemAvailabilityTransition Change(
        MenuItemOperationalAvailabilityState state,
        MenuItemOperationalAvailabilityReasonCode reasonCode,
        string reason,
        Guid actorAccountId,
        string actorRoleCode,
        string requestId,
        DateTimeOffset now)
    {
        var transition = new KioskMenuItemAvailabilityTransition
        {
            AvailabilityId = Id,
            OrganizationId = OrganizationId,
            StoreId = StoreId,
            KioskId = KioskId,
            MenuId = MenuId,
            MenuItemId = MenuItemId,
            FromState = State,
            ToState = state,
            ReasonCode = reasonCode,
            Reason = reason,
            ActorAccountId = actorAccountId,
            ActorRoleCodeSnapshot = actorRoleCode,
            RequestId = requestId,
            AvailabilityRevision = Revision + 1,
            OccurredAt = now,
            CreatedAt = now,
            CreatedByAccountId = actorAccountId
        };

        State = state;
        ReasonCode = reasonCode;
        Reason = reason;
        Revision++;
        ChangedAt = now;
        ChangedByAccountId = actorAccountId;
        UpdatedAt = now;
        UpdatedByAccountId = actorAccountId;
        Transitions.Add(transition);
        return transition;
    }
}
