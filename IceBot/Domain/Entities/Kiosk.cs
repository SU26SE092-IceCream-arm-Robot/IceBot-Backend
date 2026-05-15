using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public partial class Kiosk : BusinessEntity, IOrganizationScoped
{
    public Guid? OrganizationId { get; set; }

    public Guid StoreId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string KioskType { get; set; } = "RoboticVending";

    public KioskStatus Status { get; set; } = KioskStatus.Provisioning;

    public string? SerialNumber { get; set; }

    public string TimeZone { get; set; } = "Asia/Bangkok";

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTimeOffset? InstalledAt { get; set; }

    public DateTimeOffset? LastOnlineAt { get; set; }

    public bool SupportsOfflineMode { get; set; } = true;

    public long ConfigurationVersion { get; set; }

    public int SettingsSchemaVersion { get; set; } = 1;

    public string? SettingsJson { get; set; }

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

    public virtual Organization? Organization { get; set; }

    public virtual Store Store { get; set; } = null!;
}
