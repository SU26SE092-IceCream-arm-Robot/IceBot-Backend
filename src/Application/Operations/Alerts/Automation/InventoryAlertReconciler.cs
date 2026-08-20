using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Domain.Common.Enums;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Operations.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Operations.Alerts.Automation;

/// <summary>
/// Reconciles operational inventory balances. A physical dispenser is optional;
/// sensor topology enriches the balance but does not own sellability or refill work.
/// </summary>
public sealed class InventoryAlertReconciler(
    IInventoryAlertAutomationStore store,
    IRealtimeNotificationPublisher publisher,
    InventoryAlertAutomationOptions options,
    ILogger<InventoryAlertReconciler> logger)
{
    private const string LowCode = "INVENTORY_LOW";
    private const string EmptyCode = "INVENTORY_EMPTY";

    public async Task<InventoryAlertReconciliationResult> ReconcileAsync(
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        var changed = 0;
        var candidateFailures = 0;
        var scanSlot = observedAt.ToUnixTimeSeconds() / Math.Max(options.IntervalSeconds, 1);
        var ids = await store.ListActiveKioskIngredientInventoryIdsAsync(
            checked(options.BatchSize * options.MaxBatchesPerRun), scanSlot, cancellationToken);

        foreach (var id in ids)
        {
            try
            {
                var events = await ReconcileOneAsync(id, observedAt, cancellationToken);
                changed += events.Count;
                foreach (var evt in events)
                {
                    try
                    {
                        await publisher.PublishAlertChangedAsync(evt, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception,
                            "Inventory alert reconciliation committed alert {AlertId}, but SignalR publication failed.",
                            evt.AlertId);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                candidateFailures++;
                logger.LogError(exception,
                    "Inventory alert reconciliation failed for kiosk inventory balance {KioskIngredientInventoryId}.", id);
            }
        }

        return new InventoryAlertReconciliationResult(changed, candidateFailures);
    }

    private Task<List<AlertChangedEvent>> ReconcileOneAsync(
        Guid balanceId,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken) =>
        store.ExecuteInTransactionAsync(async ct =>
        {
            await store.AcquireBalanceLockAsync(balanceId, ct);
            var balance = await store.GetKioskIngredientInventoryAsync(balanceId, ct);
            if (balance?.Kiosk is null || !balance.IsActive) return [];

            var desiredCode = Classify(balance);
            var activeAlerts = await store.ListActiveBalanceInventoryAlertsAsync(balance.Id, ct);
            var events = new List<AlertChangedEvent>();
            var mutated = false;

            foreach (var alert in activeAlerts.Where(alert => alert.AlertCode != desiredCode))
            {
                var oldStatus = alert.Status.ToString();
                alert.Resolve(observedAt, "Inventory balance recovered or moved to a different threshold.");
                events.Add(ToEvent(alert, balance, oldStatus, observedAt));
                mutated = true;
            }

            var current = desiredCode is null
                ? null
                : activeAlerts.Where(alert => alert.AlertCode == desiredCode)
                    .OrderBy(alert => alert.RaisedAt).ThenBy(alert => alert.Id).FirstOrDefault();
            foreach (var duplicate in activeAlerts.Where(alert => alert.AlertCode == desiredCode && alert.Id != current?.Id))
            {
                var oldStatus = duplicate.Status.ToString();
                duplicate.Resolve(observedAt, "Duplicate inventory alert reconciled.");
                events.Add(ToEvent(duplicate, balance, oldStatus, observedAt));
                mutated = true;
            }

            if (desiredCode is not null && current is null)
            {
                current = Alert.RaiseFromKioskIngredientInventory(
                    balance.KioskId,
                    balance.Id,
                    desiredCode,
                    desiredCode == EmptyCode ? SeverityLevel.Error : SeverityLevel.Warning,
                    $"{balance.Ingredient.Name} is {desiredCode[10..].ToLowerInvariant()}",
                    $"Kiosk inventory is {balance.EstimatedQuantity?.ToString() ?? "unknown"} {balance.Unit}.",
                    observedAt);
                await store.AddAlertAsync(current, ct);
                events.Add(ToEvent(current, balance, null, observedAt));
                mutated = true;

                if (!(await store.ListActiveInventoryRefillTasksAsync(balance.Id, ct)).Any())
                {
                    var task = CreateAutomaticRefillTask(balance, current, observedAt);
                    await store.AddInventoryRefillTaskAsync(task, ct);
                    await store.AddInventoryRefillTaskTransitionAsync(new InventoryRefillTaskTransition
                    {
                        Id = Guid.NewGuid(),
                        InventoryRefillTaskId = task.Id,
                        ToStatus = InventoryRefillTaskStatus.Requested,
                        Reason = $"Automatically requested from {desiredCode} alert.",
                        RequestIdempotencyKey = task.RequestIdempotencyKey,
                        RequestFingerprint = task.RequestFingerprint,
                        OccurredAt = observedAt,
                        CreatedAt = observedAt
                    }, ct);
                }
            }

            if (mutated) await store.SaveChangesAsync(ct);
            return events;
        }, cancellationToken);

    private static string? Classify(KioskIngredientInventory balance)
    {
        if (!balance.EstimatedQuantity.HasValue) return null;
        if (balance.EstimatedQuantity <= 0) return EmptyCode;
        return balance.LowStockThreshold.HasValue && balance.EstimatedQuantity <= balance.LowStockThreshold
            ? LowCode
            : null;
    }

    private static InventoryRefillTask CreateAutomaticRefillTask(
        KioskIngredientInventory balance,
        Alert alert,
        DateTimeOffset observedAt)
    {
        var idempotencyKey = $"alert:{alert.Id:N}";
        return new InventoryRefillTask
        {
            Id = Guid.NewGuid(),
            OrganizationId = balance.OrganizationId,
            StoreId = balance.StoreId,
            KioskId = balance.KioskId,
            KioskIngredientInventoryId = balance.Id,
            SourceAlertId = alert.Id,
            RequestSource = InventoryRefillRequestSource.AlertAutomation,
            Unit = balance.Unit,
            RequestedAt = observedAt,
            RequestIdempotencyKey = idempotencyKey,
            RequestFingerprint = $"alert:{alert.Id:N}:{balance.Id:N}",
            CreatedAt = observedAt,
            Version = 1
        };
    }

    private static AlertChangedEvent ToEvent(Alert alert, KioskIngredientInventory balance, string? oldStatus, DateTimeOffset observedAt) => new()
    {
        AlertId = alert.Id,
        KioskId = balance.KioskId,
        OrganizationId = balance.OrganizationId,
        StoreId = balance.StoreId,
        DeviceId = null,
        AlertCode = alert.AlertCode,
        Severity = alert.Severity.ToString(),
        OldStatus = oldStatus,
        NewStatus = alert.Status.ToString(),
        UpdatedAt = observedAt,
        Version = checked((int)alert.Version),
        OccurrenceCount = alert.OccurrenceCount,
        LastOccurredAt = alert.LastOccurredAt
    };
}

public sealed record InventoryAlertReconciliationResult(int ChangedAlertCount, int CandidateFailureCount);
