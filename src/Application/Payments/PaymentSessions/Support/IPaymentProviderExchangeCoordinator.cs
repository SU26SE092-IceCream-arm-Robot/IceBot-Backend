using Application.Payments.Providers;
using Domain.Payments.Entities;
using Domain.Payments.Enums;

namespace Application.Payments.PaymentSessions.Support;

public interface IPaymentProviderExchangeCoordinator
{
    Task<PaymentProviderExchangeStartResult> StartCreateAsync(
        Guid paymentTransactionId,
        ProviderExchangeEvidence? evidence = null,
        CancellationToken cancellationToken = default);

    Task<PaymentProviderExchangeStartResult> StartLookupAsync(
        Guid paymentTransactionId,
        ProviderExchangeEvidence? evidence = null,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(
        Guid exchangeId,
        PaymentProviderExchangeOutcome outcome,
        ProviderExchangeEvidence? evidence,
        string? failureCode,
        string? failureMessage,
        Action<PaymentTransaction>? applyProjection,
        CancellationToken cancellationToken = default);
}
