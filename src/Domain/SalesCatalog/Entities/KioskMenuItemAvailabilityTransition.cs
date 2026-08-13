using Domain.Common;
using Domain.SalesCatalog.Enums;

namespace Domain.SalesCatalog.Entities;

public sealed class KioskMenuItemAvailabilityTransition : GuidEntity, IAuditable, IOrganizationScoped
{
    public Guid AvailabilityId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid StoreId { get; set; }
    public Guid KioskId { get; set; }
    public Guid MenuId { get; set; }
    public Guid MenuItemId { get; set; }
    public MenuItemOperationalAvailabilityState FromState { get; set; }
    public MenuItemOperationalAvailabilityState ToState { get; set; }
    public MenuItemOperationalAvailabilityReasonCode ReasonCode { get; set; }
    public string Reason { get; set; } = null!;
    public Guid ActorAccountId { get; set; }
    public string ActorRoleCodeSnapshot { get; set; } = null!;
    public string RequestId { get; set; } = null!;
    public long AvailabilityRevision { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedByAccountId { get; set; }
    public Guid? UpdatedByAccountId { get; set; }
}
