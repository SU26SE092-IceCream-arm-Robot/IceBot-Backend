using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Domain.Devices.Connectivity;

namespace Application.Payments.Abstractions;

public interface IPaymentStore
{
    Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<KioskConnectivityProjection?> GetKioskConnectivityAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<PaymentMethod?> GetPaymentMethodByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<List<PaymentMethod>> ListPaymentMethodsAsync(CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetPaymentTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetPaymentTransactionSnapshotAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListPendingPaymentSessionReconciliationIdsAsync(
        DateTimeOffset requestedBefore,
        DateTimeOffset retryDueAt,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<int> CountPaymentSessionInterventionsAsync(
        DateTimeOffset observedAt,
        string? provider,
        string? interventionCode,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTransaction>> ListPaymentSessionInterventionsAsync(
        DateTimeOffset observedAt,
        string? provider,
        string? interventionCode,
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

    Task<PaymentTransaction?> GetPaymentTransactionByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetActivePaymentTransactionByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetPaymentTransactionByProviderOrderCodeAsync(string provider, string providerOrderCode, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetLatestPaymentTransactionByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetLatestPaidPaymentTransactionByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTransaction>> ListPaymentTransactionsByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<PaymentCallback?> GetPaymentCallbackAsync(
        string provider,
        string providerEventId,
        CancellationToken cancellationToken = default);

    Task AcquirePaymentSessionLockAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task AcquireOrderWorkflowLockAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task ReloadOrderAsync(Order order, CancellationToken cancellationToken = default);

    Task AcquirePaymentTransactionLockAsync(Guid paymentTransactionId, CancellationToken cancellationToken = default);

    Task AcquireRefundRequestLockAsync(Guid paymentTransactionId, CancellationToken cancellationToken = default);

    Task AcquireRefundLockAsync(Guid refundId, CancellationToken cancellationToken = default);

    Task AcquirePaymentCallbackLockAsync(string provider, string providerEventId, CancellationToken cancellationToken = default);

    Task AcquireProviderPaymentLockAsync(
        string provider,
        string providerOrderCode,
        CancellationToken cancellationToken = default);

    Task AddPaymentMethodAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);

    Task AddPaymentTransactionAsync(PaymentTransaction paymentTransaction, CancellationToken cancellationToken = default);

    Task AddPaymentCallbackAsync(PaymentCallback paymentCallback, CancellationToken cancellationToken = default);

    Task AddOrderStatusHistoryAsync(OrderStatusHistory history, CancellationToken cancellationToken = default);

    Task<Refund?> GetRefundByIdAsync(Guid refundId, CancellationToken cancellationToken = default);

    Task<Refund?> GetRefundByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task AddRefundAsync(Refund refund, CancellationToken cancellationToken = default);

    Task<bool> RefundExistsForTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);

    Task<int> CountRefundsAsync(
        string? search,
        Domain.Payments.Enums.RefundStatus? status,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<List<Refund>> ListRefundsAsync(
        string? search,
        Domain.Payments.Enums.RefundStatus? status,
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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
