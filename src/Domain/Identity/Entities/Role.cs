using Domain.Common;

namespace Domain.Identity.Entities;

public partial class Role : CatalogEntity
{
    public bool IsSystemRole { get; set; }

    public int Priority { get; set; }

    public virtual ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();
}
