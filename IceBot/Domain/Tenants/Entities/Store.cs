using Domain.Common;
using Domain.Common.Enums;

namespace Domain.Tenants.Entities;

public partial class Store : BusinessEntity
{
    public Guid? OrganizationId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string StoreType { get; set; } = "Retail";

    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    public string? Country { get; set; }

    public string TimeZone { get; set; } = "Asia/Bangkok";

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public int OpeningHoursSchemaVersion { get; set; } = 1;

    public string? OpeningHoursJson { get; set; }

    public virtual ICollection<Kiosk> Kiosks { get; set; } = new List<Kiosk>();

    public virtual Organization? Organization { get; set; }
}
