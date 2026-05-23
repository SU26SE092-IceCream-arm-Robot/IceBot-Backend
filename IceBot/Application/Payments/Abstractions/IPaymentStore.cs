using Domain.Orders.Entities;
using Domain.Payments.Entities;

namespace Application.Payments.Abstractions;

public interface IPaymentStore
{
    Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<PaymentMethod?> GetPaymentMethodByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<List<PaymentMethod>> ListPaymentMethodsAsync(CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetPaymentTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetPaymentTransactionByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetPaymentTransactionByProviderOrderCodeAsync(string provider, string providerOrderCode, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetLatestPaymentTransactionByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<bool> PaymentCallbackExistsAsync(string provider, string providerEventId, CancellationToken cancellationToken = default);

    Task AddPaymentMethodAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);

    Task AddPaymentTransactionAsync(PaymentTransaction paymentTransaction, CancellationToken cancellationToken = default);

    Task AddPaymentCallbackAsync(PaymentCallback paymentCallback, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
