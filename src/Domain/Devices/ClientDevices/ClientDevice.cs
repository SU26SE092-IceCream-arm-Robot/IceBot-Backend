using Domain.Common;
using Domain.Tenants.Entities;

namespace Domain.Devices.ClientDevices;

public sealed class ClientDevice : BusinessEntity
{
    private readonly List<ClientDeviceCredential> _credentials = [];

    public Guid OrganizationId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid KioskId { get; private set; }
    public ClientDeviceType Type { get; private set; }
    public ClientDeviceStatus Status { get; private set; }
    public Guid InstallationId { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string? AppVersion { get; private set; }
    public string? Platform { get; private set; }
    public int CredentialVersion { get; private set; }
    public int SessionVersion { get; private set; }
    public int Revision { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public DateTimeOffset? DisabledAt { get; private set; }
    public DateTimeOffset? RetiredAt { get; private set; }

    public Organization Organization { get; private set; } = null!;
    public Store Store { get; private set; } = null!;
    public Kiosk Kiosk { get; private set; } = null!;
    public IReadOnlyCollection<ClientDeviceCredential> Credentials => _credentials;

    private ClientDevice() { }

    public static ClientDevice Provision(
        Kiosk kiosk,
        ClientDeviceType type,
        Guid installationId,
        string displayName,
        string? appVersion,
        string? platform,
        DateTimeOffset now,
        Guid actorAccountId)
    {
        if (kiosk.Id == Guid.Empty || kiosk.OrganizationId == Guid.Empty || kiosk.StoreId == Guid.Empty)
            throw new DomainRuleException("A persisted kiosk hierarchy is required.");
        if (installationId == Guid.Empty)
            throw new DomainRuleException("Installation id is required.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainRuleException("Display name is required.");

        return new ClientDevice
        {
            OrganizationId = kiosk.OrganizationId,
            StoreId = kiosk.StoreId,
            KioskId = kiosk.Id,
            Type = type,
            Status = ClientDeviceStatus.Active,
            InstallationId = installationId,
            DisplayName = displayName.Trim(),
            AppVersion = Normalize(appVersion),
            Platform = Normalize(platform),
            CredentialVersion = 1,
            SessionVersion = 1,
            Revision = 1,
            ActivatedAt = now,
            CreatedAt = now,
            CreatedByAccountId = actorAccountId
        };
    }

    public ClientDeviceCredential AddInitialCredential(byte[] secretHash, string hashKeyVersion, DateTimeOffset now, Guid actorAccountId)
    {
        if (_credentials.Count != 0 || CredentialVersion != 1)
            throw new DomainRuleException("An initial credential can be added only once.");

        var credential = ClientDeviceCredential.Create(Id, CredentialVersion, secretHash, hashKeyVersion, now, actorAccountId);
        _credentials.Add(credential);
        return credential;
    }

    public ClientDeviceCredential RotateCredential(byte[] secretHash, string hashKeyVersion, DateTimeOffset now, Guid actorAccountId)
    {
        EnsureNotRetired();
        var active = _credentials.SingleOrDefault(credential => credential.Status == ClientDeviceCredentialStatus.Active)
            ?? throw new DomainRuleException("Client device has no active credential.");
        active.Rotate(now, actorAccountId);
        CredentialVersion++;
        SessionVersion++;
        Revision++;
        Touch(now, actorAccountId);

        var credential = ClientDeviceCredential.Create(Id, CredentialVersion, secretHash, hashKeyVersion, now, actorAccountId);
        _credentials.Add(credential);
        return credential;
    }

    public void Disable(DateTimeOffset now, Guid actorAccountId)
    {
        EnsureNotRetired();
        if (Status == ClientDeviceStatus.Disabled)
            return;

        Status = ClientDeviceStatus.Disabled;
        DisabledAt = now;
        SessionVersion++;
        Revision++;
        Touch(now, actorAccountId);
    }

    public void Reactivate(DateTimeOffset now, Guid actorAccountId)
    {
        if (Status == ClientDeviceStatus.Retired)
            throw new DomainRuleException("A retired client device cannot be reactivated.");
        if (!_credentials.Any(credential => credential.Status == ClientDeviceCredentialStatus.Active))
            throw new DomainRuleException("An active credential is required to reactivate a client device.");
        if (Status == ClientDeviceStatus.Active)
            return;

        Status = ClientDeviceStatus.Active;
        DisabledAt = null;
        ActivatedAt = now;
        SessionVersion++;
        Revision++;
        Touch(now, actorAccountId);
    }

    public void Rebind(Kiosk kiosk, DateTimeOffset now, Guid actorAccountId)
    {
        EnsureNotRetired();
        if (kiosk.Id == Guid.Empty || kiosk.OrganizationId == Guid.Empty || kiosk.StoreId == Guid.Empty)
            throw new DomainRuleException("A persisted kiosk hierarchy is required.");

        OrganizationId = kiosk.OrganizationId;
        StoreId = kiosk.StoreId;
        KioskId = kiosk.Id;
        SessionVersion++;
        Revision++;
        Touch(now, actorAccountId);
    }

    public void Retire(DateTimeOffset now, Guid actorAccountId)
    {
        if (Status == ClientDeviceStatus.Retired)
            return;

        Status = ClientDeviceStatus.Retired;
        RetiredAt = now;
        SessionVersion++;
        Revision++;
        foreach (var credential in _credentials.Where(credential => credential.Status == ClientDeviceCredentialStatus.Active))
            credential.Revoke(now, actorAccountId);
        Touch(now, actorAccountId);
    }

    public bool TryObserve(DateTimeOffset now, TimeSpan minimumInterval)
    {
        if (LastSeenAt.HasValue && now - LastSeenAt.Value < minimumInterval)
            return false;

        LastSeenAt = now;
        return true;
    }

    public bool MatchesAuthentication(int credentialVersion, int sessionVersion) =>
        Status == ClientDeviceStatus.Active &&
        CredentialVersion == credentialVersion &&
        SessionVersion == sessionVersion &&
        _credentials.Any(credential => credential.Status == ClientDeviceCredentialStatus.Active && credential.Version == credentialVersion);

    private void EnsureNotRetired()
    {
        if (Status == ClientDeviceStatus.Retired)
            throw new DomainRuleException("A retired client device cannot be changed.");
    }

    private void Touch(DateTimeOffset now, Guid actorAccountId)
    {
        UpdatedAt = now;
        UpdatedByAccountId = actorAccountId;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
