using Application.Abstractions.Realtime;
using Application.Operations.Abstractions;
using Application.Operations.Alerts.Commands;
using Application.Operations.Alerts.Requests;
using Application.Operations.Alerts.Results;
using Application.Shared.Wrappers;
using Domain.Common;
using Domain.Common.Enums;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Domain.Tenants.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Operations;

public sealed class AlertLifecycleTests
{
    [Fact]
    public void RecordOccurrence_UpdatesCorrelationMetadataAndOnlyRaisesSeverity()
    {
        var firstOccurredAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var alert = Alert.RaiseFromDeviceEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " motor-overheat ",
            SeverityLevel.Error, "Motor overheat", "First", firstOccurredAt,
            Guid.NewGuid(), firstOccurredAt);

        var latestSourceId = Guid.NewGuid();
        var latestOccurredAt = firstOccurredAt.AddMinutes(1);
        alert.RecordOccurrence(
            latestSourceId, SeverityLevel.Critical, "Motor overheat", "Second",
            latestOccurredAt, latestOccurredAt);
        alert.RecordOccurrence(
            Guid.NewGuid(), SeverityLevel.Error, "Motor overheat", "Third",
            latestOccurredAt.AddSeconds(1), latestOccurredAt.AddSeconds(1));

        Assert.Equal("MOTOR-OVERHEAT", alert.CorrelationKey);
        Assert.Equal(3, alert.OccurrenceCount);
        Assert.Equal(latestOccurredAt.AddSeconds(1), alert.LastOccurredAt);
        Assert.Equal(SeverityLevel.Critical, alert.Severity);
        Assert.Equal("Third", alert.Message);
        Assert.Equal(3, alert.Version);
    }

    [Fact]
    public void Acknowledge_IsIdempotent_AndResolveCompletesLifecycle()
    {
        var accountId = Guid.NewGuid();
        var acknowledgedAt = DateTimeOffset.UtcNow;
        var alert = new Alert { Status = AlertStatus.Open };

        alert.Acknowledge(accountId, acknowledgedAt);
        alert.Acknowledge(accountId, acknowledgedAt.AddMinutes(1));
        alert.Resolve(acknowledgedAt.AddMinutes(2), "Motor inspected and reset.");

        Assert.Equal(AlertStatus.Resolved, alert.Status);
        Assert.Equal(accountId, alert.AcknowledgedByAccountId);
        Assert.Equal(acknowledgedAt, alert.AcknowledgedAt);
        Assert.Equal("Motor inspected and reset.", alert.ResolutionNotes);
        Assert.Equal(2, alert.Version);
    }

    [Fact]
    public async Task AcknowledgeHandler_IncrementsAlertVersionExactlyOnce()
    {
        var alert = CreateOpenAlert();
        var store = CreateSerializedStore(alert);
        var handler = new AcknowledgeAlertCommandHandler(
            store, Substitute.For<IRealtimeNotificationPublisher>());

        var result = await handler.HandleAsync(new AcknowledgeAlertCommand
        {
            AlertId = alert.Id,
            UserContext = new() { AccountId = Guid.NewGuid(), IsSystemAdmin = true }
        });

        Assert.True(result.Succeeded);
        Assert.Equal(1, alert.Version);
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveHandler_IncrementsAlertVersionExactlyOnce()
    {
        var alert = CreateOpenAlert();
        var store = CreateSerializedStore(alert);
        var handler = new ResolveAlertCommandHandler(
            store, Substitute.For<IRealtimeNotificationPublisher>());

        var result = await handler.HandleAsync(new ResolveAlertCommand
        {
            AlertId = alert.Id,
            UserContext = new() { AccountId = Guid.NewGuid(), IsSystemAdmin = true },
            Request = new ResolveAlertRequest { ResolutionNotes = "Issue corrected." }
        });

        Assert.True(result.Succeeded);
        Assert.Equal(1, alert.Version);
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SuppressedAlert_IsTerminalForAcknowledgeAndResolve()
    {
        var alert = new Alert { Status = AlertStatus.Open };
        alert.Suppress(DateTimeOffset.UtcNow, "Known maintenance window.");

        Assert.Throws<DomainRuleException>(() => alert.Acknowledge(Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<DomainRuleException>(() => alert.Resolve(DateTimeOffset.UtcNow, "Resolved"));
    }

    private static Alert CreateOpenAlert()
    {
        var kiosk = new Kiosk
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            Code = "KIOSK-1",
            Name = "Kiosk 1"
        };
        return new Alert
        {
            Id = Guid.NewGuid(),
            KioskId = kiosk.Id,
            Kiosk = kiosk,
            AlertCode = "TEST",
            Title = "Test alert",
            Status = AlertStatus.Open,
            RaisedAt = DateTimeOffset.UtcNow,
            Version = 0
        };
    }

    private static IAlertStore CreateSerializedStore(Alert alert)
    {
        var store = Substitute.For<IAlertStore>();
        store.GetAccessibleByIdAsync(
                alert.Id, true,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(alert);
        store.ExecuteSerializedAsync(
                alert.Id,
                Arg.Any<Func<CancellationToken, Task<ApiResult<AlertResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<AlertResult>>>>()(
                CancellationToken.None));
        return store;
    }
}
