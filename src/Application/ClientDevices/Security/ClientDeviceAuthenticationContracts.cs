namespace Application.ClientDevices.Security;

public static class ClientDeviceAuthenticationDefaults
{
    public const string Scheme = "ClientDeviceBearer";
    public const string ClientDeviceIdClaim = "client_device_id";
    public const string KioskIdClaim = "kiosk_id";
    public const string CredentialVersionClaim = "credential_version";
    public const string SessionVersionClaim = "session_version";
}

public interface IClientDeviceTokenIssuer
{
    string Issue(Guid clientDeviceId, Guid kioskId, int credentialVersion, int sessionVersion);
}

public interface ICurrentClientDeviceContext
{
    bool IsAuthenticated { get; }
    Guid ClientDeviceId { get; }
    Guid OrganizationId { get; }
    Guid StoreId { get; }
    Guid KioskId { get; }
    void Set(Guid clientDeviceId, Guid organizationId, Guid storeId, Guid kioskId);
}

public sealed class CurrentClientDeviceContext : ICurrentClientDeviceContext
{
    public bool IsAuthenticated { get; private set; }
    public Guid ClientDeviceId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid KioskId { get; private set; }

    public void Set(Guid clientDeviceId, Guid organizationId, Guid storeId, Guid kioskId)
    {
        if (clientDeviceId == Guid.Empty || organizationId == Guid.Empty || storeId == Guid.Empty || kioskId == Guid.Empty)
            throw new InvalidOperationException("A complete client device scope is required.");

        ClientDeviceId = clientDeviceId;
        OrganizationId = organizationId;
        StoreId = storeId;
        KioskId = kioskId;
        IsAuthenticated = true;
    }
}
