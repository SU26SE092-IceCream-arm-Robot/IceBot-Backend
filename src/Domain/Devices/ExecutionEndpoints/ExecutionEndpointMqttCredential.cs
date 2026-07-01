using Domain.Common;
using Domain.Devices.Enums;

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
        if (Status is not (ExecutionEndpointMqttCredentialStatus.Active or ExecutionEndpointMqttCredentialStatus.Failed))
            throw new DomainRuleException("Only active or failed MQTT credentials can be rotated.");
        CredentialVersion++;
        Status = ExecutionEndpointMqttCredentialStatus.PendingRotation;
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

    public void MarkRevoked(DateTimeOffset revokedAt)
    {
        if (Status == ExecutionEndpointMqttCredentialStatus.Revoked)
            throw new DomainRuleException("MQTT credential is already revoked.");
        Status = ExecutionEndpointMqttCredentialStatus.Revoked;
        RevokedAt = revokedAt;
        LastError = null;
    }
}
