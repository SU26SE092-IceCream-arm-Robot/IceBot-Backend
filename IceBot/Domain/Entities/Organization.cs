using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public partial class Organization : BusinessEntity
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? LegalName { get; set; }

    public string? TaxCode { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public string? MetadataJson { get; set; }

    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
}
