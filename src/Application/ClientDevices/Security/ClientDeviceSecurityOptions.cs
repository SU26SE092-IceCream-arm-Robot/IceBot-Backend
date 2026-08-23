namespace Application.ClientDevices.Security;

public sealed class ClientDeviceSecurityOptions
{
    public const string SectionName = "ClientDevices:Security";

    public string CurrentHashKeyVersion { get; init; } = string.Empty;
    public Dictionary<string, string> HashKeys { get; init; } = new(StringComparer.Ordinal);
    public string JwtSecret { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int TokenLifetimeMinutes { get; init; } = 20;
    public int LastSeenMinimumIntervalMinutes { get; init; } = 5;
}
