using Application.Abstractions.Realtime;
using Application.Operations.Alerts.Automation;
using Domain.Catalog.Entities;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Domain.Tenants.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace IceBot.UnitTests.Operations;

public sealed class InventoryAlertReconcilerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Empty_balance_creates_one_alert_and_audited_refill_task()
    {
        var balance = Balance(0, 20);
        var (reconciler, _, alerts, tasks) = CreateReconciler(balance);

        var changed = await reconciler.ReconcileAsync(Now);

        Assert.Equal(1, changed.ChangedAlertCount);
        var alert = Assert.Single(alerts);
        Assert.Equal("INVENTORY_EMPTY", alert.AlertCode);
        var task = Assert.Single(tasks);
        Assert.Equal(alert.Id, task.SourceAlertId);
        Assert.Equal(InventoryRefillRequestSource.AlertAutomation, task.RequestSource);
    }

    [Fact]
    public async Task Repeated_reconciliation_does_not_create_duplicate_alert_or_task()
    {
        var balance = Balance(0, 20);
        var (reconciler, _, alerts, tasks) = CreateReconciler(balance);

        await reconciler.ReconcileAsync(Now);
        var changed = await reconciler.ReconcileAsync(Now.AddMinutes(1));

        Assert.Equal(0, changed.ChangedAlertCount);
        Assert.Single(alerts);
        Assert.Single(tasks);
    }

    [Fact]
    public async Task Recovered_balance_resolves_active_inventory_alert()
    {
        var balance = Balance(100, 20);
        var alert = Alert.RaiseFromKioskIngredientInventory(
            balance.KioskId, balance.Id, "INVENTORY_LOW",
            Domain.Common.Enums.SeverityLevel.Warning, "Low", null, Now.AddMinutes(-1));
        var (reconciler, _, _, _) = CreateReconciler(balance, [alert]);

        var changed = await reconciler.ReconcileAsync(Now);

        Assert.Equal(1, changed.ChangedAlertCount);
        Assert.Equal(AlertStatus.Resolved, alert.Status);
    }

    [Fact]
    public async Task Candidate_failure_does_not_block_later_balances()
    {
        var failed = Balance(0, 20);
        var healthy = Balance(0, 20);
        var store = Substitute.For<IInventoryAlertAutomationStore>();
        store.ListActiveKioskIngredientInventoryIdsAsync(Arg.Any<int>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns([failed.Id, healthy.Id]);
        store.GetKioskIngredientInventoryAsync(failed.Id, Arg.Any<CancellationToken>()).Returns<Task<KioskIngredientInventory?>>(_ => throw new InvalidOperationException("poison balance"));
        store.GetKioskIngredientInventoryAsync(healthy.Id, Arg.Any<CancellationToken>()).Returns(healthy);
        store.ListActiveBalanceInventoryAlertsAsync(healthy.Id, Arg.Any<CancellationToken>()).Returns([]);
        store.ListActiveInventoryRefillTasksAsync(healthy.Id, Arg.Any<CancellationToken>()).Returns([]);
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<List<Application.Abstractions.Realtime.Events.AlertChangedEvent>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<List<Application.Abstractions.Realtime.Events.AlertChangedEvent>>>>()(CancellationToken.None));
        var alerts = new List<Alert>();
        store.AddAlertAsync(Arg.Do<Alert>(alerts.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var reconciler = new InventoryAlertReconciler(store, Substitute.For<IRealtimeNotificationPublisher>(),
            new InventoryAlertAutomationOptions { BatchSize = 10, MaxBatchesPerRun = 1 }, NullLogger<InventoryAlertReconciler>.Instance);

        var result = await reconciler.ReconcileAsync(Now);

        Assert.Equal(1, result.CandidateFailureCount);
        Assert.Equal(1, result.ChangedAlertCount);
        Assert.Single(alerts);
    }

    private static (InventoryAlertReconciler Reconciler, IInventoryAlertAutomationStore Store, List<Alert> Alerts, List<InventoryRefillTask> Tasks)
        CreateReconciler(KioskIngredientInventory balance, List<Alert>? existing = null)
    {
        var alerts = existing ?? [];
        var tasks = new List<InventoryRefillTask>();
        var store = Substitute.For<IInventoryAlertAutomationStore>();
        store.ListActiveKioskIngredientInventoryIdsAsync(Arg.Any<int>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns([balance.Id]);
        store.GetKioskIngredientInventoryAsync(balance.Id, Arg.Any<CancellationToken>()).Returns(balance);
        store.ListActiveBalanceInventoryAlertsAsync(balance.Id, Arg.Any<CancellationToken>()).Returns(_ => alerts.Where(alert => alert.Status is AlertStatus.Open or AlertStatus.Acknowledged).ToList());
        store.ListActiveInventoryRefillTasksAsync(balance.Id, Arg.Any<CancellationToken>()).Returns(_ => tasks.Where(task => task.Status is InventoryRefillTaskStatus.Requested or InventoryRefillTaskStatus.InProgress).ToList());
        store.AddAlertAsync(Arg.Do<Alert>(alerts.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        store.AddInventoryRefillTaskAsync(Arg.Do<InventoryRefillTask>(tasks.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        store.AddInventoryRefillTaskTransitionAsync(Arg.Any<InventoryRefillTaskTransition>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<List<Application.Abstractions.Realtime.Events.AlertChangedEvent>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<List<Application.Abstractions.Realtime.Events.AlertChangedEvent>>>>()(CancellationToken.None));
        return (new InventoryAlertReconciler(store, Substitute.For<IRealtimeNotificationPublisher>(),
            new InventoryAlertAutomationOptions { BatchSize = 10, MaxBatchesPerRun = 1 }, NullLogger<InventoryAlertReconciler>.Instance), store, alerts, tasks);
    }

    private static KioskIngredientInventory Balance(decimal quantity, decimal threshold)
    {
        var kiosk = new Kiosk { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), StoreId = Guid.NewGuid(), Code = "KIOSK-01" };
        var balance = new KioskIngredientInventory
        {
            Id = Guid.NewGuid(), OrganizationId = kiosk.OrganizationId, StoreId = kiosk.StoreId,
            KioskId = kiosk.Id, IngredientId = Guid.NewGuid(), Kiosk = kiosk,
            Ingredient = new Ingredient { Id = Guid.NewGuid(), Name = "Vanilla" }
        };
        balance.Configure("gram", quantity, threshold, null, InventoryTrackingMode.ManualEstimate, Now);
        return balance;
    }
}
