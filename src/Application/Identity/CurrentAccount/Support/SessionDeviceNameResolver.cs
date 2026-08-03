namespace Application.Identity.CurrentAccount.Support;

internal static class SessionDeviceNameResolver
{
    public static string Resolve(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Unknown device";
        }

        var browser = userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase) ? "Microsoft Edge"
            : userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase) ? "Firefox"
            : userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) ? "Chrome"
            : userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) ? "Safari"
            : "Unknown browser";
        var platform = userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) ? "Windows"
            : userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ? "Android"
            : userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ? "iOS"
            : userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase) ? "macOS"
            : userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase) ? "Linux"
            : "Unknown platform";

        return $"{browser} on {platform}";
    }
}
