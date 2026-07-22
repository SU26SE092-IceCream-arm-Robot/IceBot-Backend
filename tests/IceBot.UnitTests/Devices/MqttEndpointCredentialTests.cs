using Domain.Devices.ExecutionEndpoints;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Devices.Telemetry;

namespace IceBot.UnitTests.Devices;

public sealed class MqttEndpointCredentialTests
{
    [Fact]
    public void Lifecycle_UsesEndpointIdentityAndMonotonicVersion()
    {
        var endpointId = Guid.NewGuid();
        var credential = ExecutionEndpointMqttCredential.BeginProvision(endpointId, "MosquittoDynamicSecurity");

        Assert.Equal(endpointId.ToString("D"), credential.Username);
        Assert.Equal(1, credential.CredentialVersion);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.PendingProvision, credential.Status);

        credential.MarkActive(DateTimeOffset.UtcNow);
        credential.BeginRotation();
        Assert.Equal(2, credential.CredentialVersion);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.PendingRotation, credential.Status);

        credential.MarkActive(DateTimeOffset.UtcNow);
        credential.BeginRevocation();
        Assert.Equal(3, credential.CredentialVersion);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.PendingRevoke, credential.Status);
        credential.MarkRevoked(DateTimeOffset.UtcNow);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.Revoked, credential.Status);
        Assert.Throws<DomainRuleException>(() => credential.BeginRotation());
    }

    [Fact]
    public void FailedOperations_CanReturnToTheirOwningPendingState()
    {
        var credential = ExecutionEndpointMqttCredential.BeginProvision(
            Guid.NewGuid(), "MosquittoDynamicSecurity");
        credential.MarkFailed("Broker unavailable");

        credential.RetryFailedProvisionOrRotation();
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.PendingProvision, credential.Status);
        Assert.Equal(2, credential.CredentialVersion);

        credential.MarkActive(DateTimeOffset.UtcNow);
        credential.BeginRotation();
        credential.MarkFailed("Broker unavailable");
        credential.RetryFailedProvisionOrRotation();
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.PendingRotation, credential.Status);

        credential.MarkActive(DateTimeOffset.UtcNow);
        credential.BeginRevocation();
        credential.MarkRevocationFailed("Broker unavailable");
        credential.BeginRevocation();
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.PendingRevoke, credential.Status);
    }
}
