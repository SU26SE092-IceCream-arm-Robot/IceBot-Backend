using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Devices.Credentials.Commands;
using Application.Operations.Alerts.Automation;
using Domain.Devices.ExecutionEndpoints;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Domain.Tenants.Entities;
using IceBot.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace IceBot.UnitTests.Operations;

public sealed class MqttCredentialOperationalAlertReconcilerTests
{
    [Fact]
    public async Task Timeout_CreatesOneAlert_AndStableScanDoesNotDuplicateIt()
    {
        var fixture = CreateFixture(CreateTimeoutCredential());

        var first = await fixture.Reconciler.ReconcileAsync(
            fixture.Endpoint.Id,
            MqttCredentialReconciliationOutcome.ProvisioningMarkedFailed,
            DateTimeOffset.UtcNow);
        var second = await fixture.Reconciler.ReconcileAsync(
            fixture.Endpoint.Id,
            null,
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        var alert = Assert.Single(fixture.Alerts);
        Assert.Equal(MqttCredentialOperationalAlertReconciler.TimeoutCode, alert.AlertCode);
        Assert.Equal(1, alert.OccurrenceCount);
        await fixture.Store.Received(1).AddAlertAsync(alert, Arg.Any<CancellationToken>());
        await fixture.Publisher.Received(1).PublishAlertChangedAsync(
            Arg.Any<AlertChangedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RepeatedRevocationRetryFailure_IncrementsOccurrenceWithoutDuplicateAlert()
    {
        var fixture = CreateFixture(CreateRevokeFailedCredential());
        await fixture.Reconciler.ReconcileAsync(
            fixture.Endpoint.Id,
            MqttCredentialReconciliationOutcome.RevokeRetryFailed,
            DateTimeOffset.UtcNow);

        await fixture.Reconciler.ReconcileAsync(
            fixture.Endpoint.Id,
            MqttCredentialReconciliationOutcome.RevokeRetryFailed,
            DateTimeOffset.UtcNow.AddMinutes(5));

        var alert = Assert.Single(fixture.Alerts);
        Assert.Equal(MqttCredentialOperationalAlertReconciler.RevokeFailedCode, alert.AlertCode);
        Assert.Equal(2, alert.OccurrenceCount);
    }

    [Fact]
    public async Task CredentialRecovery_ResolvesActiveAlert()
    {
        var credential = CreateTimeoutCredential();
        var fixture = CreateFixture(credential);
        await fixture.Reconciler.ReconcileAsync(
            fixture.Endpoint.Id,
            MqttCredentialReconciliationOutcome.ProvisioningMarkedFailed,
            DateTimeOffset.UtcNow);
        credential.RetryFailedProvisionOrRotation();
        credential.MarkActive(DateTimeOffset.UtcNow.AddMinutes(1));

        var transitions = await fixture.Reconciler.ReconcileAsync(
            fixture.Endpoint.Id,
            null,
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(1, transitions);
        Assert.Equal(AlertStatus.Resolved, Assert.Single(fixture.Alerts).Status);
    }

    private static ExecutionEndpointMqttCredential CreateTimeoutCredential()
    {
        var credential = ExecutionEndpointMqttCredential.BeginProvision(
            Guid.NewGuid(), "MosquittoDynamicSecurity");
        credential.MarkFailed("MQTT provisioning operation lease expired.");
        return credential;
    }

    private static ExecutionEndpointMqttCredential CreateRevokeFailedCredential()
    {
        var credential = ExecutionEndpointMqttCredential.BeginProvision(
            Guid.NewGuid(), "MosquittoDynamicSecurity");
        credential.MarkActive(DateTimeOffset.UtcNow);
        credential.BeginRevocation();
        credential.MarkRevocationFailed("broker unavailable");
        return credential;
    }

    private static Fixture CreateFixture(ExecutionEndpointMqttCredential credential)
    {
        var kiosk = new Kiosk
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            Code = "KIOSK-MQTT-ALERT",
            Name = "MQTT alert kiosk"
        };
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kiosk.Id,
            "EDGE-MQTT-ALERT",
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        endpoint.Id = credential.KioskExecutionEndpointId;
        TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.Kiosk), kiosk);
        TestData.SetProperty(endpoint, nameof(KioskExecutionEndpoint.MqttCredential), credential);

        var alerts = new List<Alert>();
        var store = Substitute.For<IMqttCredentialAlertAutomationStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<List<AlertChangedEvent>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<List<AlertChangedEvent>>>>()(
                call.ArgAt<CancellationToken>(1)));
        store.GetEndpointAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        store.ListActiveAlertsAsync(endpoint.Id, Arg.Any<CancellationToken>())
            .Returns(_ => alerts.Where(alert =>
                    alert.Status is not (AlertStatus.Resolved or AlertStatus.Suppressed))
                .ToList());
        store.AddAlertAsync(
                Arg.Do<Alert>(alerts.Add),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        store.AcquireLockAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var publisher = Substitute.For<IRealtimeNotificationPublisher>();
        publisher.PublishAlertChangedAsync(
                Arg.Any<AlertChangedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var reconciler = new MqttCredentialOperationalAlertReconciler(
            store,
            publisher,
            NullLogger<MqttCredentialOperationalAlertReconciler>.Instance);
        return new Fixture(endpoint, alerts, store, publisher, reconciler);
    }

    private sealed record Fixture(
        KioskExecutionEndpoint Endpoint,
        List<Alert> Alerts,
        IMqttCredentialAlertAutomationStore Store,
        IRealtimeNotificationPublisher Publisher,
        MqttCredentialOperationalAlertReconciler Reconciler);
}
