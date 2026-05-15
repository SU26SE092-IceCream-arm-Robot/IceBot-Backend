using Domain.Common;

namespace Domain.Identity.Entities;

public partial class AccountDevice : BusinessEntity
{
    public Guid AccountId { get; set; }

    public string DeviceName { get; set; } = null!;

    public string Platform { get; set; } = "Unknown";

    public string? AppVersion { get; set; }

    public string? DeviceTokenHash { get; set; }

    public string? PushToken { get; set; }

    public bool IsTrusted { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
