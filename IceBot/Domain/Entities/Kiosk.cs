using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public partial class Kiosk : BusinessEntity
{
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

    public string? SettingsJson { get; set; }

    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

    public virtual Store Store { get; set; } = null!;

    public virtual ICollection<MaintenanceTicket> MaintenanceTickets { get; set; } = new List<MaintenanceTicket>();

    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<RobotJob> RobotJobs { get; set; } = new List<RobotJob>();

    public virtual ICollection<RobotProgram> RobotPrograms { get; set; } = new List<RobotProgram>();

    public virtual ICollection<KioskHeartbeat> KioskHeartbeats { get; set; } = new List<KioskHeartbeat>();

    public virtual ICollection<SyncEventInbox> SyncEventInboxes { get; set; } = new List<SyncEventInbox>();

    public virtual ICollection<SyncDeadLetter> SyncDeadLetters { get; set; } = new List<SyncDeadLetter>();
}
