using Domain.Common;
using Domain.Devices.ExecutionEndpoints;

namespace Domain.Devices.ExecutionEndpoints;

public class ExecutionEndpointMqttCredential : AuditedEntity
{
    public Guid KioskExecutionEndpointId { get; private set; }
    public string Username { get; private set; } = null!;
    public string BrokerProvider { get; private set; } = null!;
    public int CredentialVersion { get; private set; }
    public ExecutionEndpointMqttCredentialStatus Status { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? LastError { get; private set; }

    public virtual KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    private ExecutionEndpointMqttCredential() { }

    public static ExecutionEndpointMqttCredential BeginProvision(Guid endpointId, string provider)
    {
        if (endpointId == Guid.Empty || string.IsNullOrWhiteSpace(provider))
            throw new DomainRuleException("Execution endpoint and MQTT broker provider are required.");

        return new ExecutionEndpointMqttCredential
        {
            KioskExecutionEndpointId = endpointId,
            Username = endpointId.ToString("D"),
            BrokerProvider = provider.Trim(),
            CredentialVersion = 1,
            Status = ExecutionEndpointMqttCredentialStatus.PendingProvision
        };
    }

    public void BeginRotation()
    {
        if (Status != ExecutionEndpointMqttCredentialStatus.Active)
            throw new DomainRuleException("Only active MQTT credentials can begin rotation.");
        CredentialVersion++;
        Status = ExecutionEndpointMqttCredentialStatus.PendingRotation;
        LastError = null;
    }

    public void BeginReprovision()
    {
        if (Status != ExecutionEndpointMqttCredentialStatus.Revoked)
            throw new DomainRuleException("Only revoked MQTT credentials can be provisioned again.");
        CredentialVersion++;
        Status = ExecutionEndpointMqttCredentialStatus.PendingProvision;
        ActivatedAt = null;
        RevokedAt = null;
        LastError = null;
    }

    public void RetryFailedProvisionOrRotation()
    {
        if (Status != ExecutionEndpointMqttCredentialStatus.Failed)
            throw new DomainRuleException("MQTT credential provisioning or rotation has not failed.");
        CredentialVersion++;
        Status = ActivatedAt.HasValue
            ? ExecutionEndpointMqttCredentialStatus.PendingRotation
            : ExecutionEndpointMqttCredentialStatus.PendingProvision;
        LastError = null;
    }

    public void RestartPendingOperation()
    {
        if (Status is not (
            ExecutionEndpointMqttCredentialStatus.PendingProvision or
            ExecutionEndpointMqttCredentialStatus.PendingRotation or
            ExecutionEndpointMqttCredentialStatus.PendingRevoke))
            throw new DomainRuleException("MQTT credential operation is not pending.");
        CredentialVersion++;
        LastError = null;
    }

    public void MarkActive(DateTimeOffset activatedAt)
    {
        if (Status is not (ExecutionEndpointMqttCredentialStatus.PendingProvision or ExecutionEndpointMqttCredentialStatus.PendingRotation or ExecutionEndpointMqttCredentialStatus.Failed))
            throw new DomainRuleException("MQTT credential is not pending activation.");
        Status = ExecutionEndpointMqttCredentialStatus.Active;
        ActivatedAt = activatedAt;
        RevokedAt = null;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        if (Status is not (ExecutionEndpointMqttCredentialStatus.PendingProvision or ExecutionEndpointMqttCredentialStatus.PendingRotation))
            throw new DomainRuleException("Only pending MQTT credentials can fail provisioning.");
        Status = ExecutionEndpointMqttCredentialStatus.Failed;
        var normalized = string.IsNullOrWhiteSpace(error) ? "Broker provisioning failed." : error.Trim();
        LastError = normalized.Length <= 1000 ? normalized : normalized[..1000];
    }

    public void BeginRevocation()
    {
        if (Status is not (
            ExecutionEndpointMqttCredentialStatus.Active or
            ExecutionEndpointMqttCredentialStatus.Failed or
            ExecutionEndpointMqttCredentialStatus.RevokeFailed))
            throw new DomainRuleException("MQTT credential cannot begin revocation from its current state.");
        CredentialVersion++;
        Status = ExecutionEndpointMqttCredentialStatus.PendingRevoke;
        LastError = null;
    }

    public void MarkRevocationFailed(string error)
    {
        if (Status != ExecutionEndpointMqttCredentialStatus.PendingRevoke)
            throw new DomainRuleException("MQTT credential revocation is not pending.");
        Status = ExecutionEndpointMqttCredentialStatus.RevokeFailed;
        var normalized = string.IsNullOrWhiteSpace(error) ? "Broker revocation failed." : error.Trim();
        LastError = normalized.Length <= 1000 ? normalized : normalized[..1000];
    }

    public void MarkRevoked(DateTimeOffset revokedAt)
    {
        if (Status != ExecutionEndpointMqttCredentialStatus.PendingRevoke)
            throw new DomainRuleException("MQTT credential revocation is not pending.");
        Status = ExecutionEndpointMqttCredentialStatus.Revoked;
        RevokedAt = revokedAt;
        LastError = null;
    }
}
