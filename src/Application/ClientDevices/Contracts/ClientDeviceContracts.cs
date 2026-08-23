using Domain.Devices.ClientDevices;

namespace Application.ClientDevices.Contracts;

public static class ClientDeviceSessionHeaderNames
{
    public const string ClientDeviceId = "X-Client-Device-Id";
}

public sealed record ClientDeviceResult(
    Guid Id,
    Guid OrganizationId,
    Guid StoreId,
    Guid KioskId,
    string Type,
    string Status,
    Guid InstallationId,
    string DisplayName,
    string? AppVersion,
    string? Platform,
    int CredentialVersion,
    int SessionVersion,
    int Revision,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ActivatedAt,
    DateTimeOffset? DisabledAt,
    DateTimeOffset? RetiredAt);

public sealed record ProvisionClientDeviceRequest(
    Guid InstallationId,
    string Credential,
    string DisplayName,
    string? AppVersion,
    string? Platform,
    string Reason);

public sealed record ClientDeviceLifecycleRequest(int ExpectedRevision, string Reason);

public sealed record RotateClientDeviceCredentialRequest(
    int ExpectedRevision,
    string Credential,
    string Reason);

public sealed record RebindClientDeviceRequest(
    Guid TargetKioskId,
    int ExpectedRevision,
    string Reason);

public sealed record ReplaceClientDeviceRequest(
    Guid ExpectedCurrentClientDeviceId,
    int ExpectedCurrentRevision,
    Guid ReplacementInstallationId,
    string Credential,
    string DisplayName,
    string? AppVersion,
    string? Platform,
    string Reason);

public sealed record CreateClientDeviceSessionRequest(
    Guid ClientDeviceId,
    Guid InstallationId,
    string Credential,
    string? AppVersion,
    string? Platform);

public sealed record ClientDeviceSessionResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    ClientDeviceResult Device);

public static class ClientDeviceResultMapper
{
    public static ClientDeviceResult ToResult(ClientDevice device) => new(
        device.Id,
        device.OrganizationId,
        device.StoreId,
        device.KioskId,
        device.Type.ToString(),
        device.Status.ToString(),
        device.InstallationId,
        device.DisplayName,
        device.AppVersion,
        device.Platform,
        device.CredentialVersion,
        device.SessionVersion,
        device.Revision,
        device.LastSeenAt,
        device.CreatedAt,
        device.ActivatedAt,
        device.DisabledAt,
        device.RetiredAt);
}
