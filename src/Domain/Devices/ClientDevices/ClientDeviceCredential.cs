using Domain.Common;

namespace Domain.Devices.ClientDevices;

public sealed class ClientDeviceCredential : BusinessEntity
{
    public Guid ClientDeviceId { get; private set; }
    public int Version { get; private set; }
    public byte[] SecretHash { get; private set; } = null!;
    public string HashKeyVersion { get; private set; } = null!;
    public ClientDeviceCredentialStatus Status { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? RotatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public ClientDevice ClientDevice { get; private set; } = null!;

    private ClientDeviceCredential() { }

    public static ClientDeviceCredential Create(
        Guid clientDeviceId,
        int version,
        byte[] secretHash,
        string hashKeyVersion,
        DateTimeOffset now,
        Guid actorAccountId)
    {
        if (clientDeviceId == Guid.Empty || version <= 0 || secretHash.Length == 0 || string.IsNullOrWhiteSpace(hashKeyVersion))
            throw new DomainRuleException("A valid client device credential is required.");

        return new ClientDeviceCredential
        {
            ClientDeviceId = clientDeviceId,
            Version = version,
            SecretHash = secretHash,
            HashKeyVersion = hashKeyVersion.Trim(),
            Status = ClientDeviceCredentialStatus.Active,
            IssuedAt = now,
            CreatedAt = now,
            CreatedByAccountId = actorAccountId
        };
    }

    public void Rotate(DateTimeOffset now, Guid actorAccountId)
    {
        if (Status != ClientDeviceCredentialStatus.Active)
            throw new DomainRuleException("Only an active client device credential can be rotated.");

        Status = ClientDeviceCredentialStatus.Rotated;
        RotatedAt = now;
        UpdatedAt = now;
        UpdatedByAccountId = actorAccountId;
    }

    public void Revoke(DateTimeOffset now, Guid actorAccountId)
    {
        if (Status == ClientDeviceCredentialStatus.Revoked)
            return;

        Status = ClientDeviceCredentialStatus.Revoked;
        RevokedAt = now;
        UpdatedAt = now;
        UpdatedByAccountId = actorAccountId;
    }
}
