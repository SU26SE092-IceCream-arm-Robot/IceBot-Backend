using Application.Orders.Abstractions;
using Application.Orders.Management.Mapping;
using Application.Orders.Management.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Orders.Management.Queries;

public sealed class GetExecutionAttemptQueryHandler
{
    private readonly IOrderStore _orderStore;

    public GetExecutionAttemptQueryHandler(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<ApiResult<ExecutionAttemptDetailResult>> HandleAsync(
        GetExecutionAttemptQuery query,
        CancellationToken cancellationToken = default)
    {
        var command = await _orderStore.GetExecutionAttemptAsync(query.SourceCommandId, cancellationToken);
        if (command?.OrderId is null)
        {
            return ApiResult<ExecutionAttemptDetailResult>.Fail("Execution attempt not found.", 404);
        }

        var order = await _orderStore.GetOrderByIdAsync(command.OrderId.Value, cancellationToken);
        if (order is null)
        {
            return ApiResult<ExecutionAttemptDetailResult>.Fail("Order for execution attempt was not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.OrdersView,
                query.UserContext,
                order.OrganizationId,
                order.StoreId,
                order.KioskId))
        {
            return ApiResult<ExecutionAttemptDetailResult>.Fail("Access denied.", 403);
        }

        var orderRecord = (await _orderStore.ListOrderExecutionRecordsAsync(
            [command.Id],
            cancellationToken)).SingleOrDefault();
        var productionRecords = await _orderStore.ListProductionExecutionRecordsAsync(
            command.Id,
            cancellationToken);
        var adjacentAttempts = await _orderStore.ListAdjacentExecutionAttemptsAsync(
            order.Id,
            command.DispatchAttemptNo!.Value,
            cancellationToken);
        var previousAttempt = adjacentAttempts.SingleOrDefault(candidate =>
            candidate.DispatchAttemptNo == command.DispatchAttemptNo.Value - 1);
        var nextAttempt = adjacentAttempts.SingleOrDefault(candidate =>
            candidate.DispatchAttemptNo == command.DispatchAttemptNo.Value + 1);
        var redispatchHistory = command.DispatchAttemptNo > 1 && command.CreatedByAccountId.HasValue
            ? await _orderStore.GetRedispatchHistoryAsync(
                order.Id,
                command.DispatchAttemptNo.Value,
                command.CreatedAt,
                command.CreatedByAccountId.Value,
                cancellationToken)
            : null;

        return ApiResult<ExecutionAttemptDetailResult>.Success(new ExecutionAttemptDetailResult
        {
            Attempt = ExecutionAttemptResultMapper.ToResult(command, orderRecord),
            PreviousAttempt = previousAttempt is null
                ? null
                : ExecutionAttemptResultMapper.ToReference(previousAttempt),
            NextAttempt = nextAttempt is null
                ? null
                : ExecutionAttemptResultMapper.ToReference(nextAttempt),
            Provenance = new ExecutionAttemptProvenanceResult
            {
                IsRedispatch = command.DispatchAttemptNo > 1,
                RetryOfSourceCommandId = previousAttempt?.Id,
                RequestedByAccountId = command.CreatedByAccountId,
                RedispatchReason = redispatchHistory?.Reason,
                TimedOutBeforeAcceptance = string.Equals(
                    command.RejectionCode,
                    "CommandExpired",
                    StringComparison.Ordinal),
                TimedOutAt = string.Equals(command.RejectionCode, "CommandExpired", StringComparison.Ordinal)
                    ? command.RespondedAt
                    : null,
                CommandExpiryAt = command.CommandExpiryAt,
                ExecutionReportTimedOut = orderRecord?.ObservationStatus is
                    Domain.ProductionExecution.Enums.ExecutionObservationStatus.Stale or
                    Domain.ProductionExecution.Enums.ExecutionObservationStatus.Unreachable,
                ObservationRecordedAt = orderRecord?.ObservationStatus is
                    Domain.ProductionExecution.Enums.ExecutionObservationStatus.Stale or
                    Domain.ProductionExecution.Enums.ExecutionObservationStatus.Unreachable
                        ? orderRecord.CloudReceivedAt
                        : null
            },
            DeliveryAttempts = command.DeliveryAttempts
                .OrderBy(attempt => attempt.DeliveryAttemptNo)
                .Select(ExecutionAttemptResultMapper.ToResult)
                .ToArray(),
            ProductionExecutions = productionRecords.Select(ExecutionAttemptResultMapper.ToResult).ToArray()
        }, "Execution attempt retrieved successfully.");
    }
}
