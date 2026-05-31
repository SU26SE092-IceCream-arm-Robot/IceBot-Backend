using Domain.Common;
using Domain.Tenants.Entities;

namespace Domain.Identity.Entities;

public partial class AccountRole : GuidEntity
{
    public Guid AccountId { get; set; }

    public long RoleId { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset AssignedAt { get; set; }

    public Guid? AssignedByAccountId { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public virtual Organization? Organization { get; set; }

    public virtual Store? Store { get; set; }

    public virtual Kiosk? Kiosk { get; set; }

    public virtual Account? AssignedByAccount { get; set; }
}
