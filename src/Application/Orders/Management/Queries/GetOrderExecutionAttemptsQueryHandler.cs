using Application.Orders.Abstractions;
using Application.Orders.Management.Mapping;
using Application.Orders.Management.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Orders.Management.Queries;

public sealed class GetOrderExecutionAttemptsQueryHandler
{
    private readonly IOrderStore _orderStore;

    public GetOrderExecutionAttemptsQueryHandler(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<PagedResult<ExecutionAttemptResult>> HandleAsync(
        GetOrderExecutionAttemptsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.OrdersView, query.UserContext);
        var order = await _orderStore.GetManagementOrderByIdAsync(
            query.OrderId,
            query.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);
        if (order is null)
        {
            return PagedResult<ExecutionAttemptResult>.Fail("Order not found.", 404, pageNumber, pageSize);
        }

        var totalCount = await _orderStore.CountExecutionAttemptsAsync(order.Id, cancellationToken);
        var commands = await _orderStore.ListExecutionAttemptsAsync(
            order.Id,
            pageNumber,
            pageSize,
            cancellationToken);
        var commandIds = commands.Select(command => command.Id).ToArray();
        var records = await _orderStore.ListOrderExecutionRecordsAsync(commandIds, cancellationToken);
        var recordsByCommand = records.ToDictionary(record => record.SourceCommandId);

        return PagedResult<ExecutionAttemptResult>.Success(
            commands.Select(command => ExecutionAttemptResultMapper.ToResult(
                command,
                recordsByCommand.GetValueOrDefault(command.Id))),
            totalCount,
            pageNumber,
            pageSize,
            "Order execution attempts retrieved successfully.");
    }
}
