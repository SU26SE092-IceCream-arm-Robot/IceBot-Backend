using Domain.Devices.ExecutionEndpoints;
using Application.EdgeIntegration.Contracts;
using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Results;
using Application.Shared.Utils;
using Application.Shared.Wrappers;
using Domain.Devices.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Application.EdgeIntegration.Observability;
using Microsoft.Extensions.Options;

namespace Application.EdgeIntegration.Commands;

public sealed class AcknowledgeEdgeCommandCommandHandler
{
    private readonly IEdgeCommandStore _edgeCommandStore;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly ExecutionReportIngestionOptions _options;

    public AcknowledgeEdgeCommandCommandHandler(
        IEdgeCommandStore edgeCommandStore,
        IRealtimeNotificationPublisher publisher,
        IOptions<ExecutionReportIngestionOptions>? options = null)
    {
        _edgeCommandStore = edgeCommandStore;
        _publisher = publisher;
        _options = options?.Value ?? new ExecutionReportIngestionOptions();
    }

    public async Task<ApiResult<EdgeCommandAckResult>> HandleAsync(
        AcknowledgeEdgeCommandCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty || command.CommandId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.AckStatus))
        {
            return ApiResult<EdgeCommandAckResult>.Fail("Kiosk, execution endpoint, and command are required.", 400);
        }

        var endpoint = await _edgeCommandStore.GetEndpointForCommandAuthAsync(command.EndpointId, cancellationToken);
        if (endpoint is null ||
            endpoint.KioskId != command.KioskId ||
            endpoint.Status != KioskExecutionEndpointStatus.Active ||
            endpoint.CredentialBinding is null ||
            endpoint.CredentialBinding.Status != ExecutionEndpointCredentialBindingStatus.Active)
        {
            return ApiResult<EdgeCommandAckResult>.Fail("Execution endpoint authentication failed.", 401);
        }

        var edgeCommand = await _edgeCommandStore.GetByIdAsync(command.CommandId, cancellationToken);
        if (edgeCommand is null || edgeCommand.KioskId != command.KioskId || edgeCommand.TargetExecutionEndpointId != command.EndpointId)
        {
            return ApiResult<EdgeCommandAckResult>.Fail("Edge command not found.", 404);
        }

        if (command.PhysicalOutputMayHaveOccurred.HasValue &&
            (edgeCommand.CommandType != EdgeCommandType.ExecuteOrder ||
                !string.Equals(command.AckStatus.Trim(), "Rejected", StringComparison.OrdinalIgnoreCase)))
        {
            return ApiResult<EdgeCommandAckResult>.Fail(
                "Physical-output evidence is supported only for rejected execute-order commands.", 400);
        }

        var cloudReceivedAt = DateTimeOffset.UtcNow;
        var observedAt = command.AcknowledgedAt ?? cloudReceivedAt;
        var allowedSkew = TimeSpan.FromSeconds(Math.Max(_options.MaxFutureClockSkewSeconds, 0));
        if (observedAt > cloudReceivedAt.Add(allowedSkew) ||
            observedAt < edgeCommand.CreatedAt.Subtract(allowedSkew) ||
            (edgeCommand.DeliveredAt.HasValue && observedAt < edgeCommand.DeliveredAt.Value.Subtract(allowedSkew)))
        {
            return ApiResult<EdgeCommandAckResult>.Fail(
                "Acknowledgement timestamp is outside the allowed command clock-skew window.", 400);
        }
        var priorStatus = edgeCommand.Status;
        var priorDeliveryAttemptCount = edgeCommand.DeliveryAttempts.Count;
        var deliveredAt = edgeCommand.DeliveredAt;
        Order? order = null;
        OrderStatus? previousOrderStatus = null;
        if (edgeCommand.CommandType == EdgeCommandType.ExecuteOrder && edgeCommand.OrderId.HasValue)
        {
            order = await _edgeCommandStore.GetOrderForAcknowledgementAsync(
                edgeCommand.OrderId.Value,
                cancellationToken);
            if (order is null)
            {
                return ApiResult<EdgeCommandAckResult>.Fail("Order for execute-order command was not found.", 409);
            }

            previousOrderStatus = order.Status;
        }

        try
        {
            ApplyAck(edgeCommand, command, observedAt);
            ApplyOrderAck(order, edgeCommand, command);
            await EnsureProvisionalExecutionRecordAsync(
                endpoint,
                edgeCommand,
                command,
                observedAt,
                cancellationToken);
        }
        catch (Domain.Common.DomainRuleException ex)
        {
            return ApiResult<EdgeCommandAckResult>.Fail(ex.Message, 400);
        }

        var orderChanged = order is not null && previousOrderStatus != order.Status;
        if (orderChanged)
        {
            await _edgeCommandStore.AddOrderStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order!.Id,
                FromStatus = previousOrderStatus,
                ToStatus = order.Status,
                ChangedAt = observedAt,
                Reason = BuildHistoryReason(command)
            }, cancellationToken);
        }

        await _edgeCommandStore.SaveChangesAsync(cancellationToken);

        if ((edgeCommand.Status != priorStatus || edgeCommand.DeliveryAttempts.Count != priorDeliveryAttemptCount) &&
            deliveredAt.HasValue)
        {
            IceBotEdgeMetrics.RecordCommandAck(
                cloudReceivedAt - deliveredAt.Value,
                edgeCommand.CommandType.ToString(),
                command.AckStatus.Trim());
        }

        if (orderChanged)
        {
            var projection = OrderStatusProjector.ProjectFromOrder(order!);
            await _publisher.PublishOrderStatusChangedAsync(new OrderStatusChangedEvent
            {
                OrderId = order!.Id,
                OrderNumber = order.OrderNumber,
                KioskId = order.KioskId,
                OrganizationId = order.OrganizationId,
                StoreId = order.StoreId,
                OldStatus = previousOrderStatus!.Value.ToString(),
                NewStatus = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                CustomerStatus = projection.CustomerStatus,
                CustomerStatusMessage = projection.CustomerStatusMessage,
                CanRetryPayment = projection.CanRetryPayment,
                RequiresStaffSupport = projection.RequiresStaffSupport,
                UpdatedAt = observedAt,
                Version = 1
            }, cancellationToken);
        }

        return ApiResult<EdgeCommandAckResult>.Success(
            EdgeCommandAckResult.FromCommand(edgeCommand),
            "Edge command acknowledgement recorded successfully.");
    }

    private static void ApplyAck(EdgeCommand edgeCommand, AcknowledgeEdgeCommandCommand command, DateTimeOffset observedAt)
    {
        var normalizedStatus = command.AckStatus.Trim();
        if (string.Equals(normalizedStatus, "Received", StringComparison.OrdinalIgnoreCase))
        {
            edgeCommand.RejectIfExpired(observedAt);
            return;
        }

        if (string.Equals(normalizedStatus, "Accepted", StringComparison.OrdinalIgnoreCase))
        {
            if (edgeCommand.Status == EdgeCommandStatus.Accepted)
            {
                return;
            }

            edgeCommand.Accept(observedAt);
            return;
        }

        if (string.Equals(normalizedStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            if (edgeCommand.Status == EdgeCommandStatus.Rejected)
            {
                return;
            }

            edgeCommand.Reject(
                string.IsNullOrWhiteSpace(command.RejectionCode) ? "RejectedByExecutor" : command.RejectionCode,
                command.RejectionMessage,
                observedAt);
            return;
        }

        if (string.Equals(normalizedStatus, "DeliveryFailed", StringComparison.OrdinalIgnoreCase))
        {
            if (edgeCommand.Status == EdgeCommandStatus.DeliveryFailed)
            {
                return;
            }

            var nextAttemptNo = edgeCommand.DeliveryAttempts.Count + 1;
            edgeCommand.RecordDeliveryAttempt(
                nextAttemptNo,
                observedAt,
                EdgeCommandDeliveryOutcome.DeliveryFailed,
                command.RejectionCode,
                command.RejectionMessage);
            return;
        }

        if (string.Equals(normalizedStatus, "ExecutorBusy", StringComparison.OrdinalIgnoreCase))
        {
            if (edgeCommand.Status == EdgeCommandStatus.PendingDelivery &&
                edgeCommand.DeliveryAttempts.LastOrDefault()?.Outcome == EdgeCommandDeliveryOutcome.ExecutorBusy)
            {
                return;
            }

            var nextAttemptNo = edgeCommand.DeliveryAttempts.Count + 1;
            edgeCommand.RecordDeliveryAttempt(
                nextAttemptNo,
                observedAt,
                EdgeCommandDeliveryOutcome.ExecutorBusy,
                command.RejectionCode ?? "ExecutorBusy",
                command.RejectionMessage);
            return;
        }

        throw new Domain.Common.DomainRuleException("Unsupported dispatch acknowledgement status.");
    }

    private static void ApplyOrderAck(
        Order? order,
        EdgeCommand edgeCommand,
        AcknowledgeEdgeCommandCommand command)
    {
        if (order is null || edgeCommand.CommandType != EdgeCommandType.ExecuteOrder)
        {
            return;
        }

        if (string.Equals(command.AckStatus.Trim(), "Accepted", StringComparison.OrdinalIgnoreCase))
        {
            if (order.Status == OrderStatus.ReadyForExecution)
            {
                order.MarkAccepted();
            }

            return;
        }

        if (!string.Equals(command.AckStatus.Trim(), "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (command.PhysicalOutputMayHaveOccurred == true)
        {
            if (order.Status != OrderStatus.RefundRequired)
            {
                order.MarkRefundRequired(command.RejectionMessage);
            }
        }
        else if (order.Status != OrderStatus.ExecutionRejected)
        {
            order.MarkExecutionRejected(command.RejectionMessage);
        }
    }

    private static string BuildHistoryReason(AcknowledgeEdgeCommandCommand command)
    {
        var code = string.IsNullOrWhiteSpace(command.RejectionCode)
            ? null
            : $" Code: {command.RejectionCode.Trim()}.";
        return $"Execution command acknowledgement: {command.AckStatus.Trim()}.{code}".Trim();
    }

    private async Task EnsureProvisionalExecutionRecordAsync(
        Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint endpoint,
        EdgeCommand edgeCommand,
        AcknowledgeEdgeCommandCommand command,
        DateTimeOffset acknowledgedAt,
        CancellationToken cancellationToken)
    {
        if (edgeCommand.CommandType != EdgeCommandType.ExecuteOrder ||
            !string.Equals(command.AckStatus.Trim(), "Accepted", StringComparison.OrdinalIgnoreCase) ||
            !edgeCommand.OrderId.HasValue ||
            !edgeCommand.DispatchAttemptNo.HasValue ||
            await _edgeCommandStore.GetOrderExecutionRecordAsync(edgeCommand.Id, cancellationToken) is not null)
        {
            return;
        }

        var sourceExecutorId = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.FullEdgeRuntimeId
            : endpoint.ControllerId;
        if (!sourceExecutorId.HasValue)
        {
            throw new Domain.Common.DomainRuleException("Execution endpoint profile identity is missing.");
        }

        var payload = ExecuteOrderCommandPayloadCodec.ReadProvenance(edgeCommand.PayloadJson);
        var record = OrderExecutionRecord.CreateProvisionalAccepted(
            edgeCommand.OrderId.Value,
            edgeCommand.Id,
            edgeCommand.DispatchAttemptNo.Value,
            endpoint.Id,
            endpoint.ExecutionProfile,
            sourceExecutorId.Value,
            payload.ConfigurationReleaseId,
            payload.ReleaseChecksum,
            acknowledgedAt);
        await _edgeCommandStore.AddOrderExecutionRecordAsync(record, cancellationToken);
    }
}
