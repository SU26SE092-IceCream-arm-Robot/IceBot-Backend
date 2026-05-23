using Application.Payments.Providers;
using Domain.Orders.Entities;
using Domain.Payments.Entities;

namespace Application.Payments.Abstractions;

public interface IPaymentGateway
{
    string ProviderCode { get; }

    Task<ProviderPaymentSession> CreatePaymentSessionAsync(
        PaymentTransaction paymentTransaction,
        Order order,
        CancellationToken cancellationToken = default);

    Task<ProviderPaymentNotification> ParseAndVerifyNotificationAsync(
        string rawPayload,
        string? signature,
        CancellationToken cancellationToken = default);
}
