using Application.Payments.Abstractions;
using Application.Payments.Options;
using Application.Payments.Providers;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using Microsoft.Extensions.Options;

namespace Application.Payments.PaymentSessions.Support;

public enum PaymentProviderExchangeStartState
{
    Started,
    InProgress,
    RecoveryRequired
}

public sealed record PaymentProviderExchangeStartResult(
    PaymentProviderExchangeStartState State,
    Guid? ExchangeId);

/// <summary>
/// Serializes the durable lifecycle of outbound provider attempts. It never
/// interprets JSON evidence and deliberately leaves transaction projection
/// updates to the caller that owns the provider operation.
/// </summary>
public sealed class PaymentProviderExchangeCoordinator(
    IPaymentStore paymentStore,
    IOptions<PaymentProviderExchangeOptions> options,
    TimeProvider timeProvider) : IPaymentProviderExchangeCoordinator
{
    private readonly PaymentProviderExchangeOptions _options = options.Value;

    public Task<PaymentProviderExchangeStartResult> StartCreateAsync(
        Guid paymentTransactionId,
        ProviderExchangeEvidence? evidence = null,
        CancellationToken cancellationToken = default) =>
        StartAsync(paymentTransactionId, PaymentProviderExchangeOperation.CreateSession, evidence, cancellationToken);

    public Task<PaymentProviderExchangeStartResult> StartLookupAsync(
        Guid paymentTransactionId,
        ProviderExchangeEvidence? evidence = null,
        CancellationToken cancellationToken = default) =>
        StartAsync(paymentTransactionId, PaymentProviderExchangeOperation.LookupSession, evidence, cancellationToken);

    public async Task<bool> CompleteAsync(
        Guid exchangeId,
        PaymentProviderExchangeOutcome outcome,
        ProviderExchangeEvidence? evidence,
        string? failureCode,
        string? failureMessage,
        Action<PaymentTransaction>? applyProjection,
        CancellationToken cancellationToken = default)
    {
        return await paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            var paymentTransactionId = await paymentStore.GetPaymentProviderExchangePaymentTransactionIdAsync(exchangeId, ct);
            if (!paymentTransactionId.HasValue)
            {
                throw new InvalidOperationException("Payment provider exchange was not found.");
            }

            await paymentStore.AcquirePaymentTransactionLockAsync(paymentTransactionId.Value, ct);
            var exchange = await paymentStore.GetPaymentProviderExchangeByIdAsync(exchangeId, ct);
            if (exchange is null)
            {
                throw new InvalidOperationException("Payment provider exchange was not found.");
            }

            if (exchange.Status != PaymentProviderExchangeStatus.Started)
            {
                return false;
            }

            var completedAt = timeProvider.GetUtcNow();
            exchange.Complete(
                outcome,
                completedAt,
                evidence?.RequestPayloadJson,
                evidence?.ResponsePayloadJson,
                evidence?.HttpStatusCode,
                failureCode,
                failureMessage);
            applyProjection?.Invoke(exchange.PaymentTransaction);
            await paymentStore.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    private async Task<PaymentProviderExchangeStartResult> StartAsync(
        Guid paymentTransactionId,
        PaymentProviderExchangeOperation operation,
        ProviderExchangeEvidence? evidence,
        CancellationToken cancellationToken)
    {
        return await paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            await paymentStore.AcquirePaymentTransactionLockAsync(paymentTransactionId, ct);
            var payment = await paymentStore.GetPaymentTransactionByIdAsync(paymentTransactionId, ct)
                ?? throw new InvalidOperationException("Payment transaction was not found.");
            var now = timeProvider.GetUtcNow();

            if (operation == PaymentProviderExchangeOperation.LookupSession)
            {
                var createStarted = await paymentStore.GetStartedPaymentProviderExchangeAsync(
                    paymentTransactionId,
                    PaymentProviderExchangeOperation.CreateSession,
                    ct);
                if (createStarted is not null)
                {
                    if (now - createStarted.StartedAt < TimeSpan.FromSeconds(_options.StartedTimeoutSeconds))
                    {
                        return new PaymentProviderExchangeStartResult(PaymentProviderExchangeStartState.InProgress, null);
                    }

                    createStarted.Complete(
                        PaymentProviderExchangeOutcome.OutcomeUnknown,
                        now,
                        null,
                        null,
                        null,
                        "PROVIDER_EXCHANGE_STARTED_TIMEOUT",
                        "The outbound provider exchange did not complete within the configured timeout.");
                }
            }

            var started = await paymentStore.GetStartedPaymentProviderExchangeAsync(
                paymentTransactionId,
                operation,
                ct);
            if (started is not null)
            {
                if (now - started.StartedAt < TimeSpan.FromSeconds(_options.StartedTimeoutSeconds))
                {
                    return new PaymentProviderExchangeStartResult(PaymentProviderExchangeStartState.InProgress, null);
                }

                started.Complete(
                    PaymentProviderExchangeOutcome.OutcomeUnknown,
                    now,
                    null,
                    null,
                    null,
                    "PROVIDER_EXCHANGE_STARTED_TIMEOUT",
                    "The outbound provider exchange did not complete within the configured timeout.");
                await paymentStore.SaveChangesAsync(ct);
                if (operation == PaymentProviderExchangeOperation.CreateSession)
                {
                    return new PaymentProviderExchangeStartResult(PaymentProviderExchangeStartState.RecoveryRequired, null);
                }
            }

            var attemptNumber = await paymentStore.GetNextPaymentProviderExchangeAttemptNumberAsync(
                paymentTransactionId,
                operation,
                ct);
            var exchange = PaymentProviderExchange.Start(
                paymentTransactionId,
                payment.Provider,
                operation,
                attemptNumber,
                payment.ProviderOrderCode,
                now,
                evidence?.RequestPayloadJson);
            await paymentStore.AddPaymentProviderExchangeAsync(exchange, ct);
            await paymentStore.SaveChangesAsync(ct);
            return new PaymentProviderExchangeStartResult(PaymentProviderExchangeStartState.Started, exchange.Id);
        }, cancellationToken);
    }
}
