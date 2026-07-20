using Application.Abstractions.Realtime;
using Application.Operations.Alerts.Automation;
using Application.Operations.Alerts.Notifications;
using Domain.Catalog.Entities;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Domain.Tenants.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Operations;

public sealed class InventoryAlertReconcilerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EmptyTransition_CreatesOneAlertTicketAndNotification()
    {
        var state = State(IngredientLevelStatus.Low, 0);
        var (reconciler, store, _, notifier, alerts) = Reconciler(state);
        MaintenanceTicket? ticket = null;
        store.AddMaintenanceTicketAsync(Arg.Do<MaintenanceTicket>(value => ticket = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var changed = await reconciler.ReconcileAsync(Now);

        Assert.Equal(1, changed);
        var alert = Assert.Single(alerts);
        Assert.Equal("INVENTORY_EMPTY", alert.AlertCode);
        Assert.Equal(alert.Id, ticket?.AlertId);
        await notifier.Received(1).NotifyEmptyAsync(
            alert.Id, state.Kiosk!.OrganizationId, state.Kiosk.StoreId,
            state.KioskId!.Value, state.DeviceId, alert.Title, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RepeatedReconciliation_DoesNotCreateDuplicateAlertOrTicket()
    {
        var state = State(IngredientLevelStatus.Low, 0);
        var (reconciler, store, _, notifier, alerts) = Reconciler(state);

        await reconciler.ReconcileAsync(Now);
        store.MaintenanceTicketExistsForAlertAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var changed = await reconciler.ReconcileAsync(Now.AddMinutes(1));

        Assert.Equal(0, changed);
        Assert.Single(alerts);
        await notifier.Received(1).NotifyEmptyAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Recovery_ResolvesActiveInventoryAlert()
    {
        var state = State(IngredientLevelStatus.Full, 100);
        var alert = Alert.RaiseFromInventoryState(
            state.KioskId!.Value, state.DeviceId, state.Id, "INVENTORY_LOW",
            Domain.Common.Enums.SeverityLevel.Warning, "Low", null, Now.AddMinutes(-1), Guid.Empty);
        var (reconciler, _, _, _, _) = Reconciler(state, [alert]);

        var changed = await reconciler.ReconcileAsync(Now);

        Assert.Equal(1, changed);
        Assert.Equal(AlertStatus.Resolved, alert.Status);
    }

    private static (
        InventoryAlertReconciler Reconciler,
        IInventoryAlertAutomationStore Store,
        IRealtimeNotificationPublisher Publisher,
        IInventoryOperationalAlertNotifier Notifier,
        List<Alert> Alerts) Reconciler(IngredientDispenserState state, List<Alert>? existing = null)
    {
        var alerts = existing ?? [];
        var store = Substitute.For<IInventoryAlertAutomationStore>();
        store.ListActiveDispenserStateIdsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([state.Id]);
        store.GetDispenserStateAsync(state.Id, Arg.Any<CancellationToken>()).Returns(state);
        store.ListActiveInventoryAlertsAsync(state.Id, Arg.Any<CancellationToken>())
            .Returns(_ => alerts.Where(alert => alert.Status is AlertStatus.Open or AlertStatus.Acknowledged).ToList());
        store.MaintenanceTicketExistsForAlertAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        store.AddAlertAsync(Arg.Do<Alert>(alerts.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<List<Application.Abstractions.Realtime.Events.AlertChangedEvent>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<List<Application.Abstractions.Realtime.Events.AlertChangedEvent>>>>()(CancellationToken.None));
        var publisher = Substitute.For<IRealtimeNotificationPublisher>();
        var notifier = Substitute.For<IInventoryOperationalAlertNotifier>();
        var options = new InventoryAlertAutomationOptions
        {
            BatchSize = 10,
            MaxBatchesPerRun = 1,
            CreateMaintenanceTicketForEmpty = true
        };
        return (new InventoryAlertReconciler(store, publisher, notifier, options), store, publisher, notifier, alerts);
    }

    private static IngredientDispenserState State(IngredientLevelStatus level, decimal? quantity)
    {
        var kiosk = new Kiosk
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            Code = "KIOSK-01"
        };
        return new IngredientDispenserState
        {
            Id = Guid.NewGuid(),
            KioskId = kiosk.Id,
            Kiosk = kiosk,
            DeviceId = Guid.NewGuid(),
            IngredientId = Guid.NewGuid(),
            Ingredient = new Ingredient { Id = Guid.NewGuid(), Name = "Vanilla" },
            ContainerCode = "C1",
            CurrentLevelStatus = level,
            EstimatedQuantity = quantity,
            IsActive = true,
            LastMeasuredAt = Now,
            OriginNodeId = Guid.Empty,
            Version = 1
        };
    }
}
