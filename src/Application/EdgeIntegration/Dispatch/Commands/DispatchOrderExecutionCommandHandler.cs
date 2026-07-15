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
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Microsoft.Extensions.Options;

namespace Application.EdgeIntegration.Dispatch.Commands;

public sealed class DispatchOrderExecutionCommandHandler
{
    private readonly IOrderExecutionDispatchStore _store;
    private readonly OrderExecutionDispatchOptions _options;
    private readonly IEdgeCommandWakeUpPublisher _wakeUpPublisher;

    public DispatchOrderExecutionCommandHandler(
        IOrderExecutionDispatchStore store,
        IOptions<OrderExecutionDispatchOptions> options,
        IEdgeCommandWakeUpPublisher wakeUpPublisher)
    {
        _store = store;
        _options = options.Value;
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
                _store, endpoint, productionItems, cancellationToken);
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
