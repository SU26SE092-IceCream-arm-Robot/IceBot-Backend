using Application.Orders.Management.ReadModels;
using Domain.Catalog.Enums;
using Domain.Orders.Enums;

namespace Application.Orders.Management.Abstractions;

public interface IOrderFulfillmentReadStore
{
    Task<int> CountQueueItemsAsync(
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
        CancellationToken cancellationToken = default);

    Task<List<FulfillmentQueueItemReadModel>> ListQueueItemsAsync(
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
        CancellationToken cancellationToken = default);

    Task<OrderItemOwnerReadModel?> GetItemOwnerAsync(
        Guid orderItemId,
        CancellationToken cancellationToken = default);

    Task<int> CountItemStatusHistoryAsync(
        Guid orderItemId,
        CancellationToken cancellationToken = default);

    Task<List<OrderItemStatusHistoryReadModel>> ListItemStatusHistoryAsync(
        Guid orderItemId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
