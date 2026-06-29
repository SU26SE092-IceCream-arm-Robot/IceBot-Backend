using System.Text.Json;
using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.Shared.Utils;
using Domain.Devices.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionExecution.Enums;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Enums;
using Microsoft.Extensions.Options;

namespace Application.EdgeIntegration.Commands;

public sealed class ReconcileOrderExecutionTimeoutCommandHandler
{
    private readonly IOrderExecutionTimeoutStore _store;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly OrderExecutionDispatchOptions _options;

    public ReconcileOrderExecutionTimeoutCommandHandler(
        IOrderExecutionTimeoutStore store,
        IRealtimeNotificationPublisher publisher,
        IOptions<OrderExecutionDispatchOptions> options)
    {
        _store = store;
        _publisher = publisher;
        _options = options.Value;
    }

    public async Task<bool> HandleAsync(
        ReconcileOrderExecutionTimeoutCommand command,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _store.ExecuteSerializedAsync(
            command.SourceCommandId,
            ct => ReconcileLockedAsync(command, ct),
            cancellationToken);

        if (notifications?.OrderStatusChanged is not null)
        {
            await _publisher.PublishOrderStatusChangedAsync(
                notifications.OrderStatusChanged,
                cancellationToken);
        }

        if (notifications?.DashboardInvalidated is not null)
        {
            await _publisher.PublishDashboardInvalidatedAsync(
                notifications.DashboardInvalidated,
                cancellationToken);
        }

        return notifications is not null;
    }

    private async Task<ReconciliationNotifications?> ReconcileLockedAsync(
        ReconcileOrderExecutionTimeoutCommand command,
        CancellationToken cancellationToken)
    {
        var edgeCommand = await _store.GetCommandAsync(command.SourceCommandId, cancellationToken);
        if (edgeCommand?.CommandType != EdgeCommandType.ExecuteOrder || !edgeCommand.OrderId.HasValue)
        {
            return null;
        }

        var order = await _store.GetOrderAsync(edgeCommand.OrderId.Value, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (edgeCommand.Status is (EdgeCommandStatus.PendingDelivery or EdgeCommandStatus.Delivered) &&
            edgeCommand.CommandExpiryAt < command.ObservedAt &&
            edgeCommand.RejectIfExpired(command.ObservedAt))
        {
            var previousStatus = order.Status;
            if (order.Status == OrderStatus.ReadyForExecution)
            {
                order.MarkExecutionRejected("Execution command expired before executor acceptance.");
                await _store.AddOrderStatusHistoryAsync(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    FromStatus = previousStatus,
                    ToStatus = order.Status,
                    ChangedAt = command.ObservedAt,
                    Reason = "Execution command expired before executor acceptance."
                }, cancellationToken);
            }

            await _store.SaveChangesAsync(cancellationToken);
            return new ReconciliationNotifications(
                BuildDashboardEvent(order, command.ObservedAt, "OrderExecutionCommandExpired"),
                order.Status == previousStatus
                    ? null
                    : BuildOrderStatusEvent(order, previousStatus, command.ObservedAt));
        }

        if (edgeCommand.Status != EdgeCommandStatus.Accepted)
        {
            return null;
        }

        var record = await _store.GetOrderExecutionRecordAsync(edgeCommand.Id, cancellationToken)
            ?? await CreateLegacyProvisionalRecordAsync(edgeCommand, command.ObservedAt, cancellationToken);
        var cutoff = record.Status == ProductionExecutionStatus.Running
            ? command.ObservedAt.AddMinutes(-_options.RunningReportTimeoutMinutes)
            : command.ObservedAt.AddMinutes(-_options.AcceptedReportTimeoutMinutes);
        if (record.Status is not (ProductionExecutionStatus.Accepted or ProductionExecutionStatus.Running) ||
            record.LastExecutorReportedAt > cutoff)
        {
            return null;
        }

        var heartbeat = await _store.GetLatestHeartbeatAsync(
            edgeCommand.KioskId,
            record.SourceExecutorId,
            cancellationToken);
        var unreachableCutoff = command.ObservedAt.AddMinutes(-_options.HeartbeatUnreachableMinutes);
        var unreachable = heartbeat is null ||
            heartbeat.ReceivedAt < unreachableCutoff ||
            heartbeat.Status == KioskHeartbeatStatus.Offline;
        var changed = record.MarkCloudObservation(
            unreachable ? ExecutionObservationStatus.Unreachable : ExecutionObservationStatus.Stale,
            unreachable ? CustomerExecutionStatus.PendingRecovery : CustomerExecutionStatus.Delayed,
            command.ObservedAt);
        if (!changed)
        {
            return null;
        }

        await _store.SaveChangesAsync(cancellationToken);
        return new ReconciliationNotifications(
            BuildDashboardEvent(
                order,
                command.ObservedAt,
                unreachable ? "OrderExecutionUnreachable" : "OrderExecutionStale"),
            null);
    }

    private async Task<OrderExecutionRecord> CreateLegacyProvisionalRecordAsync(
        Domain.Sync.Entities.EdgeCommand edgeCommand,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken)
    {
        var endpoint = edgeCommand.TargetExecutionEndpoint;
        var sourceExecutorId = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.FullEdgeRuntimeId
            : endpoint.ControllerId;
        if (!sourceExecutorId.HasValue)
        {
            throw new Domain.Common.DomainRuleException("Execution endpoint profile identity is missing.");
        }

        using var payload = JsonDocument.Parse(edgeCommand.PayloadJson);
        var record = OrderExecutionRecord.CreateProvisionalAccepted(
            edgeCommand.OrderId!.Value,
            edgeCommand.Id,
            edgeCommand.DispatchAttemptNo!.Value,
            endpoint.Id,
            endpoint.ExecutionProfile,
            sourceExecutorId.Value,
            payload.RootElement.GetProperty("ConfigurationReleaseId").GetGuid(),
            payload.RootElement.GetProperty("ReleaseChecksum").GetString() ?? string.Empty,
            edgeCommand.RespondedAt ?? observedAt);
        await _store.AddOrderExecutionRecordAsync(record, cancellationToken);
        return record;
    }

    private static DashboardInvalidatedEvent BuildDashboardEvent(
        Order order,
        DateTimeOffset observedAt,
        string reason)
    {
        return new DashboardInvalidatedEvent
        {
            Scope = order.OrganizationId.HasValue ? "Organization" : "System",
            OrganizationId = order.OrganizationId,
            StoreId = order.StoreId,
            Reason = reason,
            UpdatedAt = observedAt
        };
    }

    private static OrderStatusChangedEvent BuildOrderStatusEvent(
        Order order,
        OrderStatus previousStatus,
        DateTimeOffset observedAt)
    {
        var projection = OrderStatusProjector.ProjectFromOrder(order);
        return new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            KioskId = order.KioskId,
            OrganizationId = order.OrganizationId,
            StoreId = order.StoreId,
            OldStatus = previousStatus.ToString(),
            NewStatus = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            CustomerStatus = projection.CustomerStatus,
            CustomerStatusMessage = projection.CustomerStatusMessage,
            CanRetryPayment = projection.CanRetryPayment,
            RequiresStaffSupport = projection.RequiresStaffSupport,
            UpdatedAt = observedAt,
            Version = 1
        };
    }

    private sealed record ReconciliationNotifications(
        DashboardInvalidatedEvent DashboardInvalidated,
        OrderStatusChangedEvent? OrderStatusChanged);
}
