using Application.Devices.Credentials.Abstractions;
using Application.Devices.Credentials.Commands;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Domain.Devices.ExecutionEndpoints;
using IceBot.UnitTests.TestSupport;
using NSubstitute;

namespace IceBot.UnitTests.Devices;

public sealed class MqttCredentialReconciliationTests
{
    [Theory]
    [InlineData(false, MqttCredentialReconciliationOutcome.ProvisioningMarkedFailed)]
    [InlineData(true, MqttCredentialReconciliationOutcome.RotationMarkedFailed)]
    public async Task StaleProvisionOrRotation_IsMarkedFailedForExplicitSecretReplacement(
        bool rotation,
        MqttCredentialReconciliationOutcome expected)
    {
        var credential = CreateActiveOrPendingCredential(rotation);
        var fixture = CreateFixture(credential);

        var outcome = await fixture.Handler.HandleAsync(
            new ReconcileStaleMqttEndpointCredentialCommand(
                fixture.Endpoint.Id,
                DateTimeOffset.UtcNow));

        Assert.Equal(expected, outcome);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.Failed, credential.Status);
        Assert.Contains("Retry", credential.LastError);
        await fixture.Provisioner.DidNotReceive().RevokeAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StalePendingRevoke_IsClaimedAndCompletedIdempotently()
    {
        var credential = CreatePendingRevoke();
        var originalVersion = credential.CredentialVersion;
        var fixture = CreateFixture(credential);

        var outcome = await fixture.Handler.HandleAsync(
            new ReconcileStaleMqttEndpointCredentialCommand(
                fixture.Endpoint.Id,
                DateTimeOffset.UtcNow));

        Assert.Equal(MqttCredentialReconciliationOutcome.Revoked, outcome);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.Revoked, credential.Status);
        Assert.Equal(originalVersion + 1, credential.CredentialVersion);
        await fixture.Provisioner.Received(1).RevokeAsync(
            fixture.Endpoint.Id, credential.Username, Arg.Any<CancellationToken>());
        await fixture.Store.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StaleRevokeFailure_WhenBrokerStillFails_RemainsRecoverable()
    {
        var credential = CreatePendingRevoke();
        credential.MarkRevocationFailed("broker unavailable");
        var fixture = CreateFixture(credential);
        fixture.Provisioner.RevokeAsync(
                fixture.Endpoint.Id, credential.Username, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("still unavailable"));

        var outcome = await fixture.Handler.HandleAsync(
            new ReconcileStaleMqttEndpointCredentialCommand(
                fixture.Endpoint.Id,
                DateTimeOffset.UtcNow));

        Assert.Equal(MqttCredentialReconciliationOutcome.RevokeRetryFailed, outcome);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.RevokeFailed, credential.Status);
        Assert.Contains("still unavailable", credential.LastError);
    }

    [Fact]
    public async Task RecentPendingOperation_IsIgnored()
    {
        var credential = ExecutionEndpointMqttCredential.BeginProvision(
            Guid.NewGuid(), "MosquittoDynamicSecurity");
        credential.CreatedAt = DateTimeOffset.UtcNow;
        var fixture = CreateFixture(credential);

        var outcome = await fixture.Handler.HandleAsync(
            new ReconcileStaleMqttEndpointCredentialCommand(
                fixture.Endpoint.Id,
                DateTimeOffset.UtcNow));

        Assert.Equal(MqttCredentialReconciliationOutcome.NoLongerStale, outcome);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.PendingProvision, credential.Status);
        await fixture.Store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static ExecutionEndpointMqttCredential CreateActiveOrPendingCredential(bool rotation)
    {
        var credential = ExecutionEndpointMqttCredential.BeginProvision(
            Guid.NewGuid(), "MosquittoDynamicSecurity");
        credential.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        if (!rotation)
            return credential;

        credential.MarkActive(DateTimeOffset.UtcNow.AddMinutes(-10));
        credential.BeginRotation();
        return credential;
    }

    private static ExecutionEndpointMqttCredential CreatePendingRevoke()
    {
        var credential = ExecutionEndpointMqttCredential.BeginProvision(
            Guid.NewGuid(), "MosquittoDynamicSecurity");
        credential.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        credential.MarkActive(DateTimeOffset.UtcNow.AddMinutes(-10));
        credential.BeginRevocation();
        return credential;
    }

    private static Fixture CreateFixture(ExecutionEndpointMqttCredential credential)
    {
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            Guid.NewGuid(),
            "EDGE-MQTT-RECONCILE",
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        endpoint.Id = credential.KioskExecutionEndpointId;
        TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.MqttCredential), credential);

        var store = Substitute.For<IExecutionEndpointStore>();
        store.GetByIdForCredentialRotationAsync(endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        store.ExecuteMqttCredentialMutationAsync(
                Arg.Any<Guid>(),
                Arg.Any<Func<CancellationToken, Task<MqttCredentialReconciliationOutcome>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<MqttCredentialReconciliationOutcome>>>()(
                call.ArgAt<CancellationToken>(2)));
        store.ExecuteMqttCredentialMutationAsync(
                Arg.Any<Guid>(),
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<bool>>>()(
                call.ArgAt<CancellationToken>(2)));

        var provisioner = Substitute.For<IMqttEndpointCredentialProvisioner>();
        provisioner.RevokeAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return new Fixture(
            endpoint,
            store,
            provisioner,
            new ReconcileStaleMqttEndpointCredentialCommandHandler(store, provisioner));
    }

    private sealed record Fixture(
        KioskExecutionEndpoint Endpoint,
        IExecutionEndpointStore Store,
        IMqttEndpointCredentialProvisioner Provisioner,
        ReconcileStaleMqttEndpointCredentialCommandHandler Handler);
}
