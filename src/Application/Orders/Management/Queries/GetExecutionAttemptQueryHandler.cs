using Application.Orders.Abstractions;
using Application.Orders.Management.Mapping;
using Application.Orders.Management.Results;
using Application.EdgeIntegration.Dispatch.Contracts;
using Application.EdgeIntegration.Reports.Services;
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
        var scope = ScopeAccessRules.GetEffectiveScope(
            ScopeRoleSets.OperationsDiagnostics,
            query.UserContext);
        var order = await _orderStore.GetManagementOrderByIdAsync(
            query.OrderId,
            query.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);
        if (order is null)
        {
            return ApiResult<ExecutionAttemptDetailResult>.Fail("Order not found.", 404);
        }

        var command = await _orderStore.GetExecutionAttemptAsync(
            order.Id,
            query.SourceCommandId,
            cancellationToken);
        if (command is null)
        {
            return ApiResult<ExecutionAttemptDetailResult>.Fail("Execution attempt not found.", 404);
        }

        var orderRecord = (await _orderStore.ListOrderExecutionRecordsAsync(
            [command.Id],
            cancellationToken)).SingleOrDefault();
        var productionRecords = await _orderStore.ListProductionExecutionRecordsAsync(
            command.Id,
            cancellationToken);
        var payload = ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(command.PayloadJson);
        var productionUnitOutcomes = payload.OrderLines.Select(line =>
        {
            var snapshot = ProductionUnitOutcomeSnapshot.Create(
                line.Quantity,
                productionRecords.Where(record => record.OrderItemId == line.OrderItemId).ToArray(),
                line.ProductionUnitStartNo);
            return new ProductionUnitOutcomeSummaryResult
            {
                OrderItemId = line.OrderItemId,
                ProductionUnitStartNo = line.ProductionUnitStartNo,
                ExpectedQuantity = line.Quantity,
                CompletedQuantity = snapshot.CompletedQuantity,
                FailedQuantity = snapshot.FailedQuantity,
                ManualInterventionQuantity = snapshot.ManualInterventionQuantity,
                InProgressQuantity = snapshot.InProgressQuantity,
                UnreportedQuantity = snapshot.UnreportedQuantity,
                AggregateStatus = snapshot.AggregateStatus?.ToString()
            };
        }).ToArray();
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
            Attempt = ExecutionAttemptResultMapper.ToDiagnostics(command, orderRecord),
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
            ProductionExecutions = productionRecords.Select(ExecutionAttemptResultMapper.ToResult).ToArray(),
            ProductionUnitOutcomes = productionUnitOutcomes
        }, "Execution attempt retrieved successfully.");
    }
}
