using Domain.Devices.ExecutionEndpoints;
using Domain.Common;
using Domain.Devices.Entities;
using Domain.Devices.Enums;

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
        credential.MarkRevoked(DateTimeOffset.UtcNow);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.Revoked, credential.Status);
        Assert.Throws<DomainRuleException>(() => credential.BeginRotation());
    }
}
