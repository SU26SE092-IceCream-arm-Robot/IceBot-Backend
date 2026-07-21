using Application.Orders.Management.Abstractions;
using Application.Orders.Management.ReadModels;
using Domain.Catalog.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Orders.Persistence;

public sealed class OrderFulfillmentReadStore(IceBotDbContext dbContext) : IOrderFulfillmentReadStore
{
    public Task<int> CountQueueItemsAsync(
        Guid? kioskId,
        FulfillmentType? fulfillmentType,
        OrderItemStatus? itemStatus,
        DateTimeOffset? paidFrom,
        DateTimeOffset? paidTo,
        bool includeTerminal,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default) =>
        ApplyQueueFilters(
                kioskId, fulfillmentType, itemStatus, paidFrom, paidTo, includeTerminal,
                isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds)
            .CountAsync(cancellationToken);

    public async Task<List<FulfillmentQueueItemReadModel>> ListQueueItemsAsync(
        Guid? kioskId,
        FulfillmentType? fulfillmentType,
        OrderItemStatus? itemStatus,
        DateTimeOffset? paidFrom,
        DateTimeOffset? paidTo,
        bool includeTerminal,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var rows = await ApplyQueueFilters(
                kioskId, fulfillmentType, itemStatus, paidFrom, paidTo, includeTerminal,
                isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds)
            .OrderBy(item => item.Order.PaidAt)
            .ThenBy(item => item.OrderId)
            .ThenBy(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new QueueRow(
                item.OrderId,
                item.Order.OrderNumber,
                item.Id,
                item.Order.OrganizationId,
                item.Order.StoreId,
                item.Order.KioskId,
                item.ProductNameSnapshot,
                item.ProductVariantNameSnapshot,
                item.Quantity,
                item.FulfillmentType,
                item.Status,
                item.Order.PaidAt,
                item.MenuItem.PreparationTimeSeconds ??
                item.ProductVariant.PreparationTimeSeconds ??
                item.Product.PreparationTimeSeconds))
            .ToListAsync(cancellationToken);

        var itemIds = rows.Select(row => row.OrderItemId).ToArray();
        var options = itemIds.Length == 0
            ? []
            : await dbContext.OrderItemOptions.AsNoTracking()
                .Where(option => itemIds.Contains(option.OrderItemId))
                .OrderBy(option => option.OptionGroupCodeSnapshot)
                .ThenBy(option => option.CodeSnapshot)
                .Select(option => new QueueOptionRow(
                    option.OrderItemId,
                    option.OptionGroupCodeSnapshot,
                    option.CodeSnapshot,
                    option.NameSnapshot))
                .ToListAsync(cancellationToken);
        var optionsByItem = options.ToLookup(option => option.OrderItemId);

        return rows.Select(row => new FulfillmentQueueItemReadModel(
            row.OrderId, row.OrderNumber, row.OrderItemId, row.OrganizationId, row.StoreId, row.KioskId,
            row.ProductName, row.ProductVariantName, row.Quantity, row.FulfillmentType, row.ItemStatus,
            row.PaidAt, row.PreparationTimeSeconds,
            optionsByItem[row.OrderItemId].Select(option => new FulfillmentQueueOptionReadModel(
                option.GroupCode, option.Code, option.Name)).ToArray())).ToList();
    }

    public Task<OrderItemOwnerReadModel?> GetItemOwnerAsync(
        Guid orderItemId,
        CancellationToken cancellationToken = default) =>
        dbContext.OrderItems.AsNoTracking()
            .Where(item => item.Id == orderItemId && item.Order.DeletedAt == null)
            .Select(item => new OrderItemOwnerReadModel(
                item.OrderId, item.Id, item.Order.OrganizationId, item.Order.StoreId, item.Order.KioskId))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> CountItemStatusHistoryAsync(
        Guid orderItemId,
        CancellationToken cancellationToken = default) =>
        dbContext.OrderItemStatusHistories.AsNoTracking()
            .CountAsync(history => history.OrderItemId == orderItemId, cancellationToken);

    public Task<List<OrderItemStatusHistoryReadModel>> ListItemStatusHistoryAsync(
        Guid orderItemId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        (from history in dbContext.OrderItemStatusHistories.AsNoTracking()
         join account in dbContext.Accounts.AsNoTracking()
             on history.ChangedByAccountId equals account.Id into accounts
         from account in accounts.DefaultIfEmpty()
         where history.OrderItemId == orderItemId
         orderby history.ChangedAt descending, history.Id descending
         select new OrderItemStatusHistoryReadModel(
             history.Id,
             history.OrderItemId,
             history.SourceEventId,
             history.ChangedByAccountId,
             account == null ? null : account.FullName ?? account.UserName,
             account == null ? null : account.Email,
             history.FromStatus,
             history.ToStatus,
             history.Reason,
             history.ChangedAt))
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    private IQueryable<OrderItem> ApplyQueueFilters(
        Guid? kioskId,
        FulfillmentType? fulfillmentType,
        OrderItemStatus? itemStatus,
        DateTimeOffset? paidFrom,
        DateTimeOffset? paidTo,
        bool includeTerminal,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds)
    {
        var query = dbContext.OrderItems.AsNoTracking().Where(item =>
            item.Order.DeletedAt == null &&
            item.Order.PaymentStatus == PaymentStatus.Paid &&
            item.FulfillmentType != FulfillmentType.MachineProduced &&
            item.Order.Status != OrderStatus.Cancelled &&
            item.Order.Status != OrderStatus.Refunded &&
            item.Order.Status != OrderStatus.Compensated &&
            item.Order.Status != OrderStatus.RefundRequired);

        if (!includeTerminal)
            query = query.Where(item =>
                item.Status != OrderItemStatus.Completed &&
                item.Status != OrderItemStatus.Cancelled &&
                item.Status != OrderItemStatus.Failed);
        if (kioskId.HasValue) query = query.Where(item => item.Order.KioskId == kioskId.Value);
        if (fulfillmentType.HasValue) query = query.Where(item => item.FulfillmentType == fulfillmentType.Value);
        if (itemStatus.HasValue) query = query.Where(item => item.Status == itemStatus.Value);
        if (paidFrom.HasValue) query = query.Where(item => item.Order.PaidAt >= paidFrom.Value);
        if (paidTo.HasValue) query = query.Where(item => item.Order.PaidAt <= paidTo.Value);

        if (!isSystemAdmin)
        {
            query = query.Where(item =>
                (item.Order.OrganizationId.HasValue && allowedOrganizationIds.Contains(item.Order.OrganizationId.Value)) ||
                (item.Order.StoreId.HasValue && allowedStoreIds.Contains(item.Order.StoreId.Value)) ||
                allowedKioskIds.Contains(item.Order.KioskId));
        }

        return query;
    }

    private sealed record QueueRow(
        Guid OrderId,
        string OrderNumber,
        Guid OrderItemId,
        Guid? OrganizationId,
        Guid? StoreId,
        Guid KioskId,
        string ProductName,
        string ProductVariantName,
        int Quantity,
        FulfillmentType FulfillmentType,
        OrderItemStatus ItemStatus,
        DateTimeOffset? PaidAt,
        int? PreparationTimeSeconds);

    private sealed record QueueOptionRow(Guid OrderItemId, string GroupCode, string Code, string Name);
}
