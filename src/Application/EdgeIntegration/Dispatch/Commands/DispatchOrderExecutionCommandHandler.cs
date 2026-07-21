using Domain.Devices.ExecutionEndpoints;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Dispatch.Contracts;
using Application.EdgeIntegration.Reports.Contracts;
using Application.EdgeIntegration.CommandDelivery.Results;
using Application.EdgeIntegration.Dispatch.Results;
using Application.EdgeIntegration.Reports.Results;
using Application.EdgeIntegration.CommandDelivery.Services;
using Application.EdgeIntegration.Dispatch.Services;
using Application.EdgeIntegration.Reports.Services;
using Application.Shared.Wrappers;
using Domain.Catalog.Enums;
using Domain.Common;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionExecution.Enums;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Microsoft.Extensions.Options;
using Application.Devices.Telemetry;

namespace Application.EdgeIntegration.Dispatch.Commands;

public sealed class DispatchOrderExecutionCommandHandler
{
    private readonly IOrderExecutionDispatchStore _store;
    private readonly OrderExecutionDispatchOptions _options;
    private readonly EdgeTelemetryIngestionOptions _telemetryOptions;
    private readonly IEdgeCommandWakeUpPublisher _wakeUpPublisher;

    public DispatchOrderExecutionCommandHandler(
        IOrderExecutionDispatchStore store,
        IOptions<OrderExecutionDispatchOptions> options,
        IEdgeCommandWakeUpPublisher wakeUpPublisher,
        IOptions<EdgeTelemetryIngestionOptions>? telemetryOptions = null)
    {
        _store = store;
        _options = options.Value;
        _telemetryOptions = telemetryOptions?.Value ?? new EdgeTelemetryIngestionOptions();
        _wakeUpPublisher = wakeUpPublisher;
    }

    public async Task<ApiResult<OrderExecutionDispatchResult>> HandleAsync(
        DispatchOrderExecutionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order execution dispatch is disabled.", 503);
        }

        if (command.OrderId == Guid.Empty || command.DispatchAttemptNo <= 0)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order and positive dispatch attempt are required.", 400);
        }

        var result = await _store.ExecuteSerializedAsync(
            command.OrderId,
            ct => DispatchLockedAsync(command, ct),
            cancellationToken);
        await PublishWakeUpAsync(result, cancellationToken);
        return result;
    }

    public async Task<ApiResult<OrderExecutionDispatchResult>> HandleRedispatchAsync(
        Guid orderId,
        Guid requestedByAccountId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order execution dispatch is disabled.", 503);
        }

        if (orderId == Guid.Empty || requestedByAccountId == Guid.Empty || string.IsNullOrWhiteSpace(reason))
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Order, requesting operator, and redispatch reason are required.", 400);
        }

        var result = await _store.ExecuteSerializedAsync(
            orderId,
            ct => RedispatchLockedAsync(orderId, requestedByAccountId, reason.Trim(), ct),
            cancellationToken);
        await PublishWakeUpAsync(result, cancellationToken);
        return result;
    }

    public async Task<ApiResult<OrderExecutionDispatchResult>> HandleRemakeAsync(
        Guid remakeRequestId,
        Guid orderId,
        Guid orderItemId,
        int productionUnitNo,
        int productionUnitQuantity,
        Guid requestedByAccountId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order execution dispatch is disabled.", 503);
        if (remakeRequestId == Guid.Empty || orderId == Guid.Empty || orderItemId == Guid.Empty ||
            productionUnitNo <= 0 || productionUnitQuantity <= 0 || requestedByAccountId == Guid.Empty ||
            string.IsNullOrWhiteSpace(reason))
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Remake identity, order item, positive unit range, operator, and reason are required.", 400);

        var result = await _store.ExecuteSerializedAsync(
            orderId,
            ct => RemakeLockedAsync(
                remakeRequestId, orderId, orderItemId, productionUnitNo, productionUnitQuantity,
                requestedByAccountId, reason.Trim(), ct),
            cancellationToken);
        await PublishWakeUpAsync(result, cancellationToken);
        return result;
    }

    private async Task<ApiResult<OrderExecutionDispatchResult>> RemakeLockedAsync(
        Guid remakeRequestId,
        Guid orderId,
        Guid orderItemId,
        int productionUnitNo,
        int productionUnitQuantity,
        Guid requestedByAccountId,
        string reason,
        CancellationToken cancellationToken)
    {
        var existing = await _store.GetCommandByIdAsync(remakeRequestId, cancellationToken);
        if (existing is not null)
        {
            if (existing.OrderId != orderId)
                return ApiResult<OrderExecutionDispatchResult>.Fail(
                    "Remake request id is already used by another order.", 409);
            if (existing.CreatedByAccountId != requestedByAccountId)
                return ApiResult<OrderExecutionDispatchResult>.Fail(
                    "Remake request id is already owned by another operator.", 409);
            var existingPayload = ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(existing.PayloadJson);
            var existingLine = existingPayload.OrderLines.SingleOrDefault();
            if (!string.Equals(existingPayload.ExecutionIntent, "Remake", StringComparison.Ordinal) ||
                existingLine?.OrderItemId != orderItemId ||
                existingLine.ProductionUnitStartNo != productionUnitNo ||
                existingLine.Quantity != productionUnitQuantity)
                return ApiResult<OrderExecutionDispatchResult>.Fail(
                    "Remake request id was reused with a different unit selection.", 409);
            return ApiResult<OrderExecutionDispatchResult>.Success(
                ToResult(existing, existingPayload.ConfigurationReleaseId, existing: true),
                "Existing production remake command returned.");
        }

        var order = await _store.GetOrderAsync(orderId, cancellationToken);
        if (order is null)
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order not found.", 404);
        await _store.AcquireKioskOperationalLockAsync(order.KioskId, cancellationToken);
        if (!await _store.IsKioskOperationalAsync(order.KioskId, cancellationToken))
            return KioskNotOperational();
        if (order.PaymentStatus != PaymentStatus.Paid || order.Status != OrderStatus.FulfillmentIssue)
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Only a paid order with an unresolved fulfillment issue can request a production remake.", 409);

        var item = order.OrderItems.SingleOrDefault(candidate => candidate.Id == orderItemId);
        if (item is null || item.FulfillmentType != FulfillmentType.MachineProduced ||
            item.Status != OrderItemStatus.Failed)
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Only a failed machine-produced order item can be remade.", 409);
        var requestedLastUnit = (long)productionUnitNo + productionUnitQuantity - 1;
        if (requestedLastUnit > item.Quantity)
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Remake unit range exceeds the order-item quantity.", 400);

        var latest = await _store.GetLatestCommandAsync(orderId, cancellationToken);
        if (latest is null || !latest.DispatchAttemptNo.HasValue)
            return ApiResult<OrderExecutionDispatchResult>.Fail("No prior execution attempt was found.", 409);
        if (latest.DispatchAttemptNo.Value >= _options.MaxDispatchAttempts)
            return ApiResult<OrderExecutionDispatchResult>.Fail("Maximum execution dispatch attempts reached.", 409);

        var records = await _store.ListProductionExecutionRecordsForOrderItemAsync(
            orderId, orderItemId, cancellationToken);
        var snapshot = ProductionUnitOutcomeSnapshot.CreateEffective(item.Quantity, records, latest);
        if (!snapshot.HasCompleteCoverage || snapshot.InProgressQuantity > 0)
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Production remake requires complete, terminal unit evidence for the source item.", 409);
        var targetRecords = Enumerable.Range(productionUnitNo, productionUnitQuantity)
            .Select(unitNo => snapshot.Units[unitNo - 1])
            .ToArray();
        if (targetRecords.Any(record => record?.Status != ProductionExecutionStatus.Failed ||
                record.PhysicalOutputState != PhysicalOutputState.No))
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Every remake unit must have failed with confirmed no physical output.", 409);
        var sourceCommandIds = targetRecords.Select(record => record!.SourceCommandId).Distinct().ToArray();
        if (sourceCommandIds.Length != 1)
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Remake units must originate from one execution command.", 409);
        var remakeOfSourceCommandId = sourceCommandIds[0];

        var endpoints = await _store.ListActiveEndpointsAsync(order.KioskId, cancellationToken);
        var candidates = new List<OrderExecutionDispatchCandidate>();
        foreach (var endpoint in endpoints)
        {
            var candidate = await OrderExecutionDispatchPlanner.TryBuildCandidateAsync(
                _store, endpoint, [item], ReadinessReceivedAfter(), cancellationToken);
            if (candidate is not null) candidates.Add(candidate);
        }
        if (candidates.Count != 1)
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                candidates.Count == 0
                    ? "No ready execution endpoint can perform the remake."
                    : "Multiple execution endpoints can perform the remake; endpoint selection is ambiguous.",
                409);

        var selected = candidates[0];
        await _store.AcquireEndpointAdmissionLockAsync(selected.Endpoint.Id, cancellationToken);
        if (await _store.CountActiveCommandsAsync(selected.Endpoint.Id, cancellationToken) >=
            _options.MaxActiveCommandsPerEndpoint)
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Execution endpoint still has an active command or its admission queue is full.", 409);

        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddMinutes(_options.CommandExpiryMinutes);
        var attemptNo = latest.DispatchAttemptNo.Value + 1;
        var basePayload = ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(
            OrderExecutionDispatchPlanner.BuildPayload(
                remakeRequestId, attemptNo, order, selected, [item], expiry));
        var baseLine = basePayload.OrderLines.Single();
        var payload = ExecuteOrderCommandPayloadCodec.Serialize(basePayload with
        {
            ExecutionIntent = "Remake",
            RemakeOfSourceCommandId = remakeOfSourceCommandId,
            OrderLines =
            [
                baseLine with
                {
                    ProductionUnitStartNo = productionUnitNo,
                    Quantity = productionUnitQuantity
                }
            ]
        });
        var edgeCommand = EdgeCommand.Create(
            EdgeCommandType.ExecuteOrder, order.KioskId, selected.Endpoint.Id, payload, now,
            order.Id, attemptNo, expiry);
        edgeCommand.Id = remakeRequestId;
        edgeCommand.CreatedByAccountId = requestedByAccountId;

        await _store.AddOrderStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.Id,
            ChangedByAccountId = requestedByAccountId,
            FromStatus = order.Status,
            ToStatus = order.Status,
            ChangedAt = now,
            Reason = $"Production remake attempt {attemptNo}, units {productionUnitNo}-{requestedLastUnit}: {reason}"
        }, cancellationToken);
        await _store.AddCommandAsync(edgeCommand, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);
        return ApiResult<OrderExecutionDispatchResult>.Success(
            ToResult(edgeCommand, selected.Release.Id, existing: false),
            "Production remake command created successfully.", 201);
    }

    private async Task<ApiResult<OrderExecutionDispatchResult>> RedispatchLockedAsync(
        Guid orderId,
        Guid requestedByAccountId,
        string reason,
        CancellationToken cancellationToken)
    {
        var latest = await _store.GetLatestCommandAsync(orderId, cancellationToken);
        if (latest is null || !latest.DispatchAttemptNo.HasValue)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("No prior execution attempt was found.", 409);
        }

        if (latest.Status is EdgeCommandStatus.PendingDelivery or EdgeCommandStatus.Delivered or EdgeCommandStatus.Accepted)
        {
            if (latest.CreatedByAccountId == requestedByAccountId)
            {
                return ApiResult<OrderExecutionDispatchResult>.Success(
                    ToResult(latest, ExecuteOrderCommandPayloadCodec.ReadProvenance(latest.PayloadJson).ConfigurationReleaseId, existing: true),
                    "Existing operator redispatch attempt returned.");
            }

            return ApiResult<OrderExecutionDispatchResult>.Fail("The latest execution attempt is still active.", 409);
        }

        if (latest.DispatchAttemptNo.Value >= _options.MaxDispatchAttempts)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Maximum execution dispatch attempts reached.", 409);
        }

        var order = await _store.GetOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order not found.", 404);
        }

        var eligible = latest.Status == EdgeCommandStatus.DeliveryFailed ||
            (latest.Status == EdgeCommandStatus.Rejected && order.Status == OrderStatus.ExecutionRejected);
        if (!eligible)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Redispatch is allowed only after delivery failure or rejection before physical output.", 409);
        }

        return await DispatchLockedAsync(
            new DispatchOrderExecutionCommand
            {
                OrderId = orderId,
                DispatchAttemptNo = latest.DispatchAttemptNo.Value + 1
            },
            cancellationToken,
            new RedispatchContext(requestedByAccountId, reason));
    }

    private async Task<ApiResult<OrderExecutionDispatchResult>> DispatchLockedAsync(
        DispatchOrderExecutionCommand command,
        CancellationToken cancellationToken,
        RedispatchContext? redispatch = null)
    {
        var existing = await _store.GetCommandAsync(command.OrderId, command.DispatchAttemptNo, cancellationToken);
        if (existing is not null)
        {
            return ApiResult<OrderExecutionDispatchResult>.Success(
                ToResult(existing, ExecuteOrderCommandPayloadCodec.ReadProvenance(existing.PayloadJson).ConfigurationReleaseId, existing: true),
                "Existing order execution command returned for idempotent dispatch retry.");
        }

        var order = await _store.GetOrderAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order not found.", 404);
        }

        await _store.AcquireKioskOperationalLockAsync(order.KioskId, cancellationToken);
        if (!await _store.IsKioskOperationalAsync(order.KioskId, cancellationToken))
        {
            return KioskNotOperational();
        }

        var canPrepareRejectedOrder = redispatch is not null && order.Status == OrderStatus.ExecutionRejected;
        if (order.PaymentStatus != PaymentStatus.Paid ||
            (order.Status != OrderStatus.ReadyForFulfillment && !canPrepareRejectedOrder))
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Only paid orders ready for execution can be dispatched.", 409);
        }

        var productionItems = order.OrderItems
            .Where(item => item.FulfillmentType == FulfillmentType.MachineProduced)
            .OrderBy(item => item.Id)
            .ToArray();
        if (productionItems.Length == 0)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Order has no machine-produced items to dispatch.", 409);
        }

        if (productionItems.Any(item => item.RecipeId is null))
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Every machine-produced order item requires a recipe before dispatch.", 409);
        }

        var endpoints = await _store.ListActiveEndpointsAsync(order.KioskId, cancellationToken);
        var candidates = new List<OrderExecutionDispatchCandidate>();
        foreach (var endpoint in endpoints)
        {
            var candidate = await OrderExecutionDispatchPlanner.TryBuildCandidateAsync(
                _store, endpoint, productionItems, ReadinessReceivedAfter(), cancellationToken);
            if (candidate is not null)
            {
                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "No ready, safe execution endpoint with compatible capabilities and active configuration covers every machine-produced order item.", 409);
        }

        if (candidates.Count > 1)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Multiple active execution endpoints can execute this order; endpoint selection is ambiguous.", 409);
        }

        var selected = candidates[0];
        await _store.AcquireEndpointAdmissionLockAsync(selected.Endpoint.Id, cancellationToken);
        var activeCommandCount = await _store.CountActiveCommandsAsync(selected.Endpoint.Id, cancellationToken);
        if (activeCommandCount >= _options.MaxActiveCommandsPerEndpoint)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail("Execution endpoint admission queue is full.", 409);
        }

        var now = DateTimeOffset.UtcNow;
        var expiry = command.CommandExpiryAt ?? now.AddMinutes(_options.CommandExpiryMinutes);
        if (expiry <= now)
        {
            return ApiResult<OrderExecutionDispatchResult>.Fail(
                "Command expiry must be later than dispatch time.", 400);
        }

        var commandId = GuidId.New();
        var payload = OrderExecutionDispatchPlanner.BuildPayload(
            commandId, command.DispatchAttemptNo, order, selected, productionItems, expiry);
        var edgeCommand = EdgeCommand.Create(
            EdgeCommandType.ExecuteOrder,
            order.KioskId,
            selected.Endpoint.Id,
            payload,
            now,
            order.Id,
            command.DispatchAttemptNo,
            expiry);
        edgeCommand.Id = commandId;
        edgeCommand.CreatedByAccountId = redispatch?.RequestedByAccountId;

        if (redispatch is not null)
        {
            var previousStatus = order.Status;
            if (order.Status == OrderStatus.ExecutionRejected)
            {
                order.PrepareRedispatch();
            }

            await _store.AddOrderStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.Id,
                ChangedByAccountId = redispatch.RequestedByAccountId,
                FromStatus = previousStatus,
                ToStatus = order.Status,
                ChangedAt = now,
                Reason = $"Redispatch attempt {command.DispatchAttemptNo}: {redispatch.Reason}"
            }, cancellationToken);
        }

        await _store.AddCommandAsync(edgeCommand, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);

        return ApiResult<OrderExecutionDispatchResult>.Success(
            ToResult(edgeCommand, selected.Release.Id, existing: false),
            "Order execution command created successfully.",
            201);
    }

    private DateTimeOffset ReadinessReceivedAfter() =>
        DateTimeOffset.UtcNow.AddSeconds(-_telemetryOptions.ReadinessTimeoutSeconds);

    private static ApiResult<OrderExecutionDispatchResult> KioskNotOperational() =>
        ApiResult<OrderExecutionDispatchResult>.Fail(
            "Kiosk is not operational. The paid order remains queued and will not be dispatched until operations resume.",
            409);

    private static OrderExecutionDispatchResult ToResult(
        EdgeCommand command,
        Guid releaseId,
        bool existing) => new()
        {
            OrderId = command.OrderId!.Value,
            EdgeCommandId = command.Id,
            KioskExecutionEndpointId = command.TargetExecutionEndpointId,
            ConfigurationReleaseId = releaseId,
            DispatchAttemptNo = command.DispatchAttemptNo!.Value,
            CommandExpiryAt = command.CommandExpiryAt!.Value,
            Existing = existing
        };

    private Task PublishWakeUpAsync(
        ApiResult<OrderExecutionDispatchResult> result,
        CancellationToken cancellationToken)
    {
        if (!result.Succeeded || result.Data is null)
        {
            return Task.CompletedTask;
        }

        return _wakeUpPublisher.TryPublishAsync(
            new EdgeCommandWakeUp(
                result.Data.EdgeCommandId,
                result.Data.KioskExecutionEndpointId,
                EdgeCommandType.ExecuteOrder,
                DateTimeOffset.UtcNow),
            cancellationToken);
    }

    private sealed record RedispatchContext(Guid RequestedByAccountId, string Reason);
}
