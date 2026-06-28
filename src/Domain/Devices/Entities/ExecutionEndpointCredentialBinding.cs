using Domain.Common;
using Domain.Devices.Enums;

namespace Domain.Devices.Entities;

public class ExecutionEndpointCredentialBinding : AuditedEntity
{
    public Guid KioskExecutionEndpointId { get; private set; }

    public ExecutionEndpointAuthenticationMode AuthenticationMode { get; private set; }

    public string CredentialReference { get; private set; } = null!;

    public string? PublicKeyPem { get; private set; }

    public ExecutionEndpointCredentialBindingStatus Status { get; private set; } = ExecutionEndpointCredentialBindingStatus.Provisioned;

    public DateTimeOffset ProvisionedAt { get; private set; }

    public DateTimeOffset? ActivatedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public virtual KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    private ExecutionEndpointCredentialBinding()
    {
    }

    public static ExecutionEndpointCredentialBinding CreateProvisioned(
        Guid kioskExecutionEndpointId,
        ExecutionEndpointAuthenticationMode authenticationMode,
        string credentialReference,
        DateTimeOffset provisionedAt,
        string? publicKeyPem = null)
    {
        if (kioskExecutionEndpointId == Guid.Empty || string.IsNullOrWhiteSpace(credentialReference))
        {
            throw new DomainRuleException("Execution endpoint and credential reference are required.");
        }

        if ((authenticationMode == ExecutionEndpointAuthenticationMode.MutualTls && !string.IsNullOrWhiteSpace(publicKeyPem)) ||
            (authenticationMode == ExecutionEndpointAuthenticationMode.SignedCommandTls && string.IsNullOrWhiteSpace(publicKeyPem)))
        {
            throw new DomainRuleException("Mutual TLS uses a certificate fingerprint; signed-command TLS requires a public key.");
        }

        return new ExecutionEndpointCredentialBinding
        {
            KioskExecutionEndpointId = kioskExecutionEndpointId,
            AuthenticationMode = authenticationMode,
            CredentialReference = credentialReference.Trim(),
            PublicKeyPem = string.IsNullOrWhiteSpace(publicKeyPem) ? null : publicKeyPem.Trim(),
            ProvisionedAt = provisionedAt
        };
    }

    internal void Activate(DateTimeOffset activatedAt)
    {
        if (Status != ExecutionEndpointCredentialBindingStatus.Provisioned)
        {
            throw new DomainRuleException("Only provisioned credential bindings can be activated.");
        }

        Status = ExecutionEndpointCredentialBindingStatus.Active;
        ActivatedAt = activatedAt;
    }

    internal void Revoke(DateTimeOffset revokedAt)
    {
        if (Status == ExecutionEndpointCredentialBindingStatus.Revoked)
        {
            throw new DomainRuleException("Credential binding is already revoked.");
        }

        Status = ExecutionEndpointCredentialBindingStatus.Revoked;
        RevokedAt = revokedAt;
    }
}
