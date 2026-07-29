using Application.Orders.Management.Abstractions;
using Application.Orders.Management.Results;
using Application.Orders.Management.Rules;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Orders.Management.Queries;

public sealed class ListFulfillmentQueueQueryHandler(IOrderFulfillmentReadStore fulfillment)
{
    public async Task<PagedResult<FulfillmentQueueItemResult>> HandleAsync(
        ListFulfillmentQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FulfillmentType == Domain.Catalog.Enums.FulfillmentType.MachineProduced)
            return PagedResult<FulfillmentQueueItemResult>.Fail(
                "Machine-produced work is read through execution attempts, not the staff fulfillment queue.",
                400, query.PageNumber, query.PageSize);
        if (query.PaidFrom.HasValue && query.PaidTo.HasValue && query.PaidFrom > query.PaidTo)
            return PagedResult<FulfillmentQueueItemResult>.Fail(
                "Paid-from must not be later than paid-to.", 400, query.PageNumber, query.PageSize);

        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.OrdersView, query.UserContext);
        var totalCount = await fulfillment.CountQueueItemsAsync(
            query.KioskId, query.FulfillmentType, query.ItemStatus, query.PaidFrom, query.PaidTo,
            query.IncludeTerminal, query.UserContext.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds, cancellationToken);
        var rows = await fulfillment.ListQueueItemsAsync(
            query.KioskId, query.FulfillmentType, query.ItemStatus, query.PaidFrom, query.PaidTo,
            query.IncludeTerminal, query.UserContext.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds,
            pageNumber, pageSize, cancellationToken);

        var observedAt = DateTimeOffset.UtcNow;
        return PagedResult<FulfillmentQueueItemResult>.Success(
            rows.Select(row =>
            {
                var sla = FulfillmentSlaRules.Project(
                    row.PaidAt, row.PreparationTimeSeconds, row.ItemStatus, observedAt);
                return new FulfillmentQueueItemResult(
                    row.OrderId, row.OrderNumber, row.OrderItemId, row.KioskId,
                    row.ProductName, row.ProductVariantName, row.Quantity, row.FulfillmentType,
                    row.ItemStatus, row.PaidAt, row.PreparationTimeSeconds,
                    sla.ExpectedReadyAt, sla.Status,
                    row.SelectedOptions.Select(option => new FulfillmentQueueOptionResult(
                        option.GroupCode, option.Code, option.Name)).ToArray());
            }),
            totalCount, pageNumber, pageSize);
    }
}
