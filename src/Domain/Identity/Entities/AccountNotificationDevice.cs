using Domain.Common;

namespace Domain.Identity.Entities;

/// <summary>
/// A registered push-notification installation for an account. This is not a
/// trusted login device or session-security record.
/// </summary>
public partial class AccountNotificationDevice : BusinessEntity
{
    public Guid AccountId { get; set; }

    /// <summary>
    /// Stable client-generated installation identifier used for registration upserts.
    /// </summary>
    public Guid InstallationId { get; set; }

    public string Platform { get; set; } = "Unknown";

    public string? PushToken { get; set; }

    /// <summary>
    /// Hash of <see cref="PushToken"/> used only for uniqueness and de-duplication.
    /// </summary>
    public string? PushTokenHash { get; set; }

    public string? DeviceName { get; set; }

    public string? AppVersion { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>
    /// Set when the account unregisters the device or the provider rejects its token.
    /// </summary>
    public DateTimeOffset? InvalidatedAt { get; set; }

    public string? InvalidationReason { get; set; }

    public virtual Account Account { get; set; } = null!;
}
