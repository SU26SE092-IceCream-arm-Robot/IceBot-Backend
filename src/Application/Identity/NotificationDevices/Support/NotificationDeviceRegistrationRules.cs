using System.Security.Cryptography;
using System.Text;

namespace Application.Identity.NotificationDevices.Support;

internal static class NotificationDeviceRegistrationRules
{
    private static readonly IReadOnlyDictionary<string, string> SupportedPlatforms =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["android"] = "Android",
            ["ios"] = "iOS",
            ["web"] = "Web"
        };

    public static bool TryNormalize(
        string? platform,
        string? pushToken,
        string? deviceName,
        string? appVersion,
        out NormalizedNotificationDeviceRegistration registration,
        out string? error)
    {
        registration = new NormalizedNotificationDeviceRegistration(
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            null);
        error = null;

        if (string.IsNullOrWhiteSpace(platform) ||
            !SupportedPlatforms.TryGetValue(platform.Trim(), out var normalizedPlatform))
        {
            error = "Platform must be Android, iOS, or Web.";
            return false;
        }

        var normalizedToken = pushToken?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            error = "Push token is required.";
            return false;
        }

        if (normalizedToken.Length > 4_096)
        {
            error = "Push token exceeds the maximum length.";
            return false;
        }

        var normalizedDeviceName = TrimToNull(deviceName);
        var normalizedAppVersion = TrimToNull(appVersion);
        if (normalizedDeviceName?.Length > 500 || normalizedAppVersion?.Length > 500)
        {
            error = "Device name or app version exceeds the maximum length.";
            return false;
        }

        registration = new NormalizedNotificationDeviceRegistration(
            normalizedPlatform,
            normalizedToken,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedToken))),
            normalizedDeviceName,
            normalizedAppVersion);
        return true;
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal sealed record NormalizedNotificationDeviceRegistration(
    string Platform,
    string PushToken,
    string PushTokenHash,
    string? DeviceName,
    string? AppVersion);
