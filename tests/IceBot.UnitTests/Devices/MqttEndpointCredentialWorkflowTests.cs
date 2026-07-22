using Application.Devices.Credentials.Abstractions;
using Application.Devices.Credentials.Commands;
using Application.Devices.Credentials.Results;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Shared.Wrappers;
using Domain.Devices.ExecutionEndpoints;
using Domain.Tenants.Entities;
using IceBot.UnitTests.TestSupport;
using NSubstitute;

namespace IceBot.UnitTests.Devices;

public sealed class MqttEndpointCredentialWorkflowTests
{
    [Fact]
    public async Task Provision_CommitsPendingBeforeBrokerAndActivatesPreparedVersion()
    {
        var fixture = CreateFixture();
        var handler = new ProvisionMqttEndpointCredentialCommandHandler(
            fixture.Store, fixture.Provisioner);

        var result = await handler.HandleAsync(new ProvisionMqttEndpointCredentialCommand
        {
            EndpointId = fixture.Endpoint.Id,
            KioskId = fixture.Endpoint.KioskId,
            UserContext = TestData.SystemAdmin()
        });

        Assert.True(result.Succeeded);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.Active, fixture.Endpoint.MqttCredential!.Status);
        Assert.Equal(1, result.Data!.CredentialVersion);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.Password));
        Received.InOrder(() =>
        {
            fixture.Store.SaveChangesAsync(Arg.Any<CancellationToken>());
            fixture.Provisioner.ProvisionOrReplaceAsync(
                fixture.Endpoint.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            fixture.Store.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Provision_BrokerFailure_CommitsFailedRecoveryState()
    {
        var fixture = CreateFixture();
        fixture.Provisioner.ProvisionOrReplaceAsync(
                fixture.Endpoint.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("broker unavailable"));
        var handler = new ProvisionMqttEndpointCredentialCommandHandler(
            fixture.Store, fixture.Provisioner);

        var result = await handler.HandleAsync(new ProvisionMqttEndpointCredentialCommand
        {
            EndpointId = fixture.Endpoint.Id,
            KioskId = fixture.Endpoint.KioskId,
            UserContext = TestData.SystemAdmin()
        });

        Assert.False(result.Succeeded);
        Assert.Equal(503, result.StatusCode);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.Failed, fixture.Endpoint.MqttCredential!.Status);
        Assert.Contains("broker unavailable", fixture.Endpoint.MqttCredential.LastError);
        await fixture.Store.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Provision_Cancellation_LeavesDurablePendingStateForStaleRecovery()
    {
        var fixture = CreateFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        fixture.Provisioner.ProvisionOrReplaceAsync(
                fixture.Endpoint.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new OperationCanceledException(cancellation.Token));
        var handler = new ProvisionMqttEndpointCredentialCommandHandler(
            fixture.Store, fixture.Provisioner);

        await Assert.ThrowsAsync<OperationCanceledException>(() => handler.HandleAsync(
            new ProvisionMqttEndpointCredentialCommand
            {
                EndpointId = fixture.Endpoint.Id,
                KioskId = fixture.Endpoint.KioskId,
                UserContext = TestData.SystemAdmin()
            }, cancellation.Token));

        Assert.Equal(
            ExecutionEndpointMqttCredentialStatus.PendingProvision,
            fixture.Endpoint.MqttCredential!.Status);
        await fixture.Store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Provision_RecentPendingOperation_RejectsConcurrentBrokerMutation()
    {
        var credential = ExecutionEndpointMqttCredential.BeginProvision(
            Guid.NewGuid(), "MosquittoDynamicSecurity");
        credential.CreatedAt = DateTimeOffset.UtcNow;
        var fixture = CreateFixture(credential);
        var handler = new ProvisionMqttEndpointCredentialCommandHandler(
            fixture.Store, fixture.Provisioner);

        var result = await handler.HandleAsync(new ProvisionMqttEndpointCredentialCommand
        {
            EndpointId = fixture.Endpoint.Id,
            KioskId = fixture.Endpoint.KioskId,
            UserContext = TestData.SystemAdmin()
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        await fixture.Provisioner.DidNotReceive().ProvisionOrReplaceAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Provision_StalePendingOperation_IsReclaimedWithNewVersion()
    {
        var credential = ExecutionEndpointMqttCredential.BeginProvision(
            Guid.NewGuid(), "MosquittoDynamicSecurity");
        credential.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var fixture = CreateFixture(credential);
        var handler = new ProvisionMqttEndpointCredentialCommandHandler(
            fixture.Store, fixture.Provisioner);

        var result = await handler.HandleAsync(new ProvisionMqttEndpointCredentialCommand
        {
            EndpointId = fixture.Endpoint.Id,
            KioskId = fixture.Endpoint.KioskId,
            UserContext = TestData.SystemAdmin()
        });

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Data!.CredentialVersion);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.Active, credential.Status);
    }

    [Fact]
    public async Task Revoke_BrokerFailure_CommitsRevokeFailedState()
    {
        var credential = ExecutionEndpointMqttCredential.BeginProvision(
            Guid.NewGuid(), "MosquittoDynamicSecurity");
        credential.MarkActive(DateTimeOffset.UtcNow);
        var fixture = CreateFixture(credential);
        fixture.Provisioner.RevokeAsync(
                fixture.Endpoint.Id, credential.Username, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("broker unavailable"));
        var handler = new RevokeMqttEndpointCredentialCommandHandler(
            fixture.Store, fixture.Provisioner);

        var result = await handler.HandleAsync(new RevokeMqttEndpointCredentialCommand
        {
            EndpointId = fixture.Endpoint.Id,
            KioskId = fixture.Endpoint.KioskId,
            UserContext = TestData.SystemAdmin()
        });

        Assert.False(result.Succeeded);
        Assert.Equal(503, result.StatusCode);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.RevokeFailed, credential.Status);
        await fixture.Store.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static WorkflowFixture CreateFixture(
        ExecutionEndpointMqttCredential? credential = null)
    {
        var kiosk = new Kiosk
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            Code = "KIOSK-MQTT",
            Name = "MQTT kiosk"
        };
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kiosk.Id,
            "EDGE-MQTT",
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        endpoint.Id = credential?.KioskExecutionEndpointId ?? Guid.NewGuid();
        TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.Kiosk), kiosk);
        if (credential is not null)
        {
            TestData.SetProperty(credential, nameof(ExecutionEndpointMqttCredential.KioskExecutionEndpointId), endpoint.Id);
            TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.MqttCredential), credential);
        }

        var store = Substitute.For<IExecutionEndpointStore>();
        ConfigureInlineTransactions(store);
        store.GetByKioskIdForCredentialRotationAsync(
                kiosk.Id, endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        store.GetByIdForCredentialRotationAsync(endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(endpoint);
        store.AddMqttCredentialAsync(
                Arg.Do<ExecutionEndpointMqttCredential>(value =>
                    TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.MqttCredential), value)),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var provisioner = Substitute.For<IMqttEndpointCredentialProvisioner>();
        provisioner.ProviderName.Returns("MosquittoDynamicSecurity");
        provisioner.GetSubscribeTopic(endpoint.Id).Returns($"icebot/execution-endpoints/{endpoint.Id:D}/commands/available");
        provisioner.ProvisionOrReplaceAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        provisioner.RevokeAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return new WorkflowFixture(endpoint, store, provisioner);
    }

    private static void ConfigureInlineTransactions(IExecutionEndpointStore store)
    {
        store.ExecuteMqttCredentialMutationAsync(
                Arg.Any<Guid>(),
                Arg.Any<Func<CancellationToken, Task<ApiResult<object>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<object>>>>()(
                call.ArgAt<CancellationToken>(2)));
        store.ExecuteMqttCredentialMutationAsync(
                Arg.Any<Guid>(),
                Arg.Any<Func<CancellationToken, Task<ApiResult<MqttEndpointCredentialResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<MqttEndpointCredentialResult>>>>()(
                call.ArgAt<CancellationToken>(2)));
        store.ExecuteMqttCredentialMutationAsync(
                Arg.Any<Guid>(),
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<bool>>>()(
                call.ArgAt<CancellationToken>(2)));
    }

    private sealed record WorkflowFixture(
        KioskExecutionEndpoint Endpoint,
        IExecutionEndpointStore Store,
        IMqttEndpointCredentialProvisioner Provisioner);
}
