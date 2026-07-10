using Application.Orders.Management.Results;
using Domain.Orders.Entities;
using Domain.ProductionExecution.Projections;
using Domain.SalesCatalog.Entities;
using Domain.Sync.Entities;
using Domain.Tenants.Entities;
using Application.SalesCatalog.ReadModels;
using Application.Orders.PlaceOrder.ReadModels;

namespace Application.Orders.Abstractions;

public interface IOrderStore
{
    Task<OrderOverviewResult> GetOrderOverviewAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        Domain.Orders.Enums.OrderStatus? status,
        Guid? kioskId,
        int take,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<MenuItem?> GetMenuItemByIdAsync(Guid menuItemId, CancellationToken cancellationToken = default);

    Task<List<MenuItemProductOptionReadModel>> ListMenuItemProductOptionsAsync(
        Guid menuItemId,
        CancellationToken cancellationToken = default);

    Task<List<ProductOptionIngredientRequirementReadModel>> ListProductOptionIngredientRequirementsAsync(
        IReadOnlyCollection<Guid> productOptionIds,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveProductionRouteAsync(
        Guid kioskId,
        Guid productVariantId,
        Guid recipeId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<int> CountOrdersAsync(
        string? search,
        Domain.Orders.Enums.OrderStatus? status,
        Domain.Orders.Enums.PaymentStatus? paymentStatus,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<List<Order>> ListOrdersAsync(
        string? search,
        Domain.Orders.Enums.OrderStatus? status,
        Domain.Orders.Enums.PaymentStatus? paymentStatus,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountOrderStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<List<OrderStatusHistory>> ListOrderStatusHistoryAsync(
        Guid orderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountExecutionAttemptsAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<List<EdgeCommand>> ListExecutionAttemptsAsync(
        Guid orderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<EdgeCommand?> GetExecutionAttemptAsync(
        Guid sourceCommandId,
        CancellationToken cancellationToken = default);

    Task<List<EdgeCommand>> ListAdjacentExecutionAttemptsAsync(
        Guid orderId,
        int dispatchAttemptNo,
        CancellationToken cancellationToken = default);

    Task<OrderStatusHistory?> GetRedispatchHistoryAsync(
        Guid orderId,
        int dispatchAttemptNo,
        DateTimeOffset commandCreatedAt,
        Guid requestedByAccountId,
        CancellationToken cancellationToken = default);

    Task<List<OrderExecutionRecord>> ListOrderExecutionRecordsAsync(
        IReadOnlyCollection<Guid> sourceCommandIds,
        CancellationToken cancellationToken = default);

    Task<OrderExecutionRecord?> GetLatestOrderExecutionRecordAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<List<ProductionExecutionRecord>> ListProductionExecutionRecordsAsync(
        Guid sourceCommandId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetOrderByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task AcquireIdempotencyLockAsync(string scopedIdempotencyKey, CancellationToken cancellationToken = default);

    Task<Order?> GetOrderByClientOrderIdAsync(Guid kioskId, string clientOrderId, CancellationToken cancellationToken = default);

    Task AddOrderAsync(Order order, CancellationToken cancellationToken = default);

    Task AddOrderStatusHistoryAsync(OrderStatusHistory history, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
