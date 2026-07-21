using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Domain.Common.Enums;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Application.Operations.Alerts.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Operations.Alerts.Automation;

public sealed class InventoryAlertReconciler(
    IInventoryAlertAutomationStore store,
    IRealtimeNotificationPublisher publisher,
    IInventoryOperationalAlertNotifier notifier,
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
        var scanSlot = observedAt.ToUnixTimeSeconds() /
            Math.Max(options.IntervalSeconds, 1);
        var ids = await store.ListActiveDispenserStateIdsAsync(
            checked(options.BatchSize * options.MaxBatchesPerRun),
            scanSlot,
            cancellationToken);
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
                    "Inventory alert reconciliation failed for dispenser state {DispenserStateId}.",
                    id);
            }
        }

        return new InventoryAlertReconciliationResult(changed, candidateFailures);
    }

    private Task<List<AlertChangedEvent>> ReconcileOneAsync(
        Guid dispenserStateId,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken) =>
        store.ExecuteInTransactionAsync(async ct =>
        {
            await store.AcquireLockAsync(dispenserStateId, ct);
            var state = await store.GetDispenserStateAsync(dispenserStateId, ct);
            if (state?.KioskId is null || state.Kiosk is null || !state.IsActive) return [];

            var desiredCode = Classify(state);
            var activeAlerts = await store.ListActiveInventoryAlertsAsync(state.Id, ct);
            var events = new List<AlertChangedEvent>();
            var mutated = false;

            foreach (var alert in activeAlerts.Where(alert => alert.AlertCode != desiredCode))
            {
                var oldStatus = alert.Status.ToString();
                alert.Resolve(observedAt, "Inventory level recovered or moved to a different threshold.");
                events.Add(ToEvent(alert, state, oldStatus, observedAt));
                mutated = true;
            }

            var current = desiredCode is null
                ? null
                : activeAlerts
                    .Where(alert => alert.AlertCode == desiredCode)
                    .OrderBy(alert => alert.RaisedAt)
                    .ThenBy(alert => alert.Id)
                    .FirstOrDefault();
            foreach (var duplicate in activeAlerts.Where(alert =>
                         alert.AlertCode == desiredCode && alert.Id != current?.Id))
            {
                var oldStatus = duplicate.Status.ToString();
                duplicate.Resolve(observedAt, "Duplicate inventory alert reconciled.");
                events.Add(ToEvent(duplicate, state, oldStatus, observedAt));
                mutated = true;
            }
            if (desiredCode is not null && current is null)
            {
                var severity = desiredCode == EmptyCode ? SeverityLevel.Error : SeverityLevel.Warning;
                current = Alert.RaiseFromInventoryState(
                    state.KioskId.Value,
                    state.DeviceId,
                    state.Id,
                    desiredCode,
                    severity,
                    $"{state.Ingredient.Name} is {desiredCode[10..].ToLowerInvariant()}",
                    $"Container {state.ContainerCode} requires inventory attention.",
                    observedAt,
                    state.OriginNodeId);
                await store.AddAlertAsync(current, ct);
                events.Add(ToEvent(current, state, null, observedAt));
                mutated = true;
                if (desiredCode == EmptyCode)
                {
                    await notifier.NotifyEmptyAsync(
                        current.Id,
                        state.Kiosk.OrganizationId,
                        state.Kiosk.StoreId,
                        state.KioskId.Value,
                        state.DeviceId,
                        current.Title,
                        ct);
                }
            }

            if (current is not null && desiredCode == EmptyCode && options.CreateMaintenanceTicketForEmpty &&
                !await store.MaintenanceTicketExistsForAlertAsync(current.Id, ct))
            {
                await store.AddMaintenanceTicketAsync(CreateTicket(current, state, observedAt), ct);
                mutated = true;
            }

            if (mutated)
            {
                await store.SaveChangesAsync(ct);
            }
            return events;
        }, cancellationToken);

    private static string? Classify(IngredientDispenserState state)
    {
        if (state.EstimatedQuantity is <= 0)
            return EmptyCode;
        return state.CurrentLevelStatus == IngredientLevelStatus.Low ? LowCode : null;
    }

    private static MaintenanceTicket CreateTicket(
        Alert alert,
        IngredientDispenserState state,
        DateTimeOffset observedAt) => new()
    {
        OrganizationId = state.Kiosk!.OrganizationId,
        StoreId = state.Kiosk.StoreId,
        KioskId = state.KioskId!.Value,
        DeviceId = state.DeviceId,
        AlertId = alert.Id,
        TicketNumber = $"AUTO-{observedAt:yyyyMMddHHmmss}-{alert.Id.ToString("N")[..8].ToUpperInvariant()}",
        IssueCode = EmptyCode,
        Title = alert.Title,
        Description = alert.Message,
        Priority = MaintenancePriority.High,
        Status = MaintenanceTicketStatus.Open,
        ReportedAt = observedAt,
        OriginNodeId = alert.OriginNodeId,
        Version = 1,
        SyncedAt = observedAt
    };

    private static AlertChangedEvent ToEvent(
        Alert alert,
        IngredientDispenserState state,
        string? oldStatus,
        DateTimeOffset observedAt) => new()
    {
        AlertId = alert.Id,
        KioskId = state.KioskId!.Value,
        OrganizationId = state.Kiosk!.OrganizationId,
        StoreId = state.Kiosk.StoreId,
        DeviceId = state.DeviceId,
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
