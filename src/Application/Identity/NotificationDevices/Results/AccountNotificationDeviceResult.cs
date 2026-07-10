namespace Application.Identity.NotificationDevices.Results;

public sealed class AccountNotificationDeviceResult
{
    public Guid InstallationId { get; init; }

    public string Platform { get; init; } = string.Empty;

    public string? DeviceName { get; init; }

    public string? AppVersion { get; init; }

    public DateTimeOffset? LastSeenAt { get; init; }

    public bool IsActive { get; init; }
}
