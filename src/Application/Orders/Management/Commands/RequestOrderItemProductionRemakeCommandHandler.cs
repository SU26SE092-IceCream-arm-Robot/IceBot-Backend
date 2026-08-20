using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Dispatch.Results;
using Application.Orders.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Orders.Management.Commands;

public sealed class RequestOrderItemProductionRemakeCommandHandler
{
    private readonly IOrderStore _orderStore;
    private readonly DispatchOrderExecutionCommandHandler _dispatchHandler;

    public RequestOrderItemProductionRemakeCommandHandler(
        IOrderStore orderStore,
        DispatchOrderExecutionCommandHandler dispatchHandler)
    {
        _orderStore = orderStore;
        _dispatchHandler = dispatchHandler;
    }

    public async Task<ApiResult<OrderExecutionDispatchResult>> HandleAsync(
        RequestOrderItemProductionRemakeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserContext.AccountId == Guid.Empty)
            return ApiResult<OrderExecutionDispatchResult>.Fail("Authenticated operator is required.", 401);
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.OrdersInterventionManage, command.UserContext);
        var order = await _orderStore.GetManagementOrderByIdAsync(
            command.OrderId,
            command.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);
        if (order is null)
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order not found.", 404);
        if (order.OrderItems.All(item => item.Id != command.OrderItemId))
            return ApiResult<OrderExecutionDispatchResult>.Fail("Order item not found on the scoped order.", 404);

        return await _dispatchHandler.HandleRemakeAsync(
            command.RemakeRequestId,
            command.OrderId,
            command.OrderItemId,
            command.ProductionUnitNo,
            command.ProductionUnitQuantity,
            command.UserContext.AccountId,
            command.Reason,
            cancellationToken,
            command.ProductionIncidentId);
    }
}
