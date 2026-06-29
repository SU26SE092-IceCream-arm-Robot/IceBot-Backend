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

        return ApiResult<ExecutionAttemptDetailResult>.Success(new ExecutionAttemptDetailResult
        {
            Attempt = ExecutionAttemptResultMapper.ToResult(command, orderRecord),
            ProductionExecutions = productionRecords.Select(ExecutionAttemptResultMapper.ToResult).ToArray()
        }, "Execution attempt retrieved successfully.");
    }
}
