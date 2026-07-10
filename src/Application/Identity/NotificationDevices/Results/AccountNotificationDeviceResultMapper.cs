using Domain.Identity.Entities;

namespace Application.Identity.NotificationDevices.Results;

internal static class AccountNotificationDeviceResultMapper
{
    public static AccountNotificationDeviceResult ToResult(AccountNotificationDevice device) => new()
    {
        InstallationId = device.InstallationId,
        Platform = device.Platform,
        DeviceName = device.DeviceName,
        AppVersion = device.AppVersion,
        LastSeenAt = device.LastSeenAt,
        IsActive = device.InvalidatedAt is null
    };
}
