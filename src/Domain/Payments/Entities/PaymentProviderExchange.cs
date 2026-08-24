using Domain.Common;
using Domain.Payments.Enums;

namespace Domain.Payments.Entities;

/// <summary>
/// Immutable evidence for one outbound provider operation. This is deliberately
/// separate from the mutable PaymentTransaction workflow projection and from
/// inbound PaymentCallback evidence.
/// </summary>
public sealed class PaymentProviderExchange : AppendOnlyEntity
{
    public Guid PaymentTransactionId { get; private set; }

    public string Provider { get; private set; } = null!;

    public PaymentProviderExchangeOperation Operation { get; private set; }

    public int AttemptNumber { get; private set; }

    public PaymentProviderExchangeStatus Status { get; private set; }

    public PaymentProviderExchangeOutcome? Outcome { get; private set; }

    public string? ProviderOrderCodeSnapshot { get; private set; }

    public string? RequestPayloadJson { get; private set; }

    public string? ResponsePayloadJson { get; private set; }

    public int? HttpStatusCode { get; private set; }

    public string? FailureCode { get; private set; }

    public string? FailureMessage { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public PaymentTransaction PaymentTransaction { get; private set; } = null!;

    private PaymentProviderExchange()
    {
    }

    public static PaymentProviderExchange Start(
        Guid paymentTransactionId,
        string provider,
        PaymentProviderExchangeOperation operation,
        int attemptNumber,
        string? providerOrderCode,
        DateTimeOffset startedAt,
        string? requestPayloadJson = null)
    {
        if (paymentTransactionId == Guid.Empty)
            throw new DomainRuleException("Payment transaction is required for a provider exchange.");
        if (string.IsNullOrWhiteSpace(provider))
            throw new DomainRuleException("Provider is required for a provider exchange.");
        if (!Enum.IsDefined(operation))
            throw new DomainRuleException("Provider exchange operation is invalid.");
        if (attemptNumber <= 0)
            throw new DomainRuleException("Provider exchange attempt number must be positive.");
        if (startedAt == default)
            throw new DomainRuleException("Provider exchange start time is required.");

        return new PaymentProviderExchange
        {
            PaymentTransactionId = paymentTransactionId,
            Provider = provider.Trim(),
            Operation = operation,
            AttemptNumber = attemptNumber,
            ProviderOrderCodeSnapshot = Normalize(providerOrderCode, 100),
            RequestPayloadJson = NormalizeJson(requestPayloadJson),
            Status = PaymentProviderExchangeStatus.Started,
            StartedAt = startedAt,
            CreatedAt = startedAt
        };
    }

    public void Complete(
        PaymentProviderExchangeOutcome outcome,
        DateTimeOffset completedAt,
        string? requestPayloadJson,
        string? responsePayloadJson,
        int? httpStatusCode,
        string? failureCode = null,
        string? failureMessage = null)
    {
        if (Status != PaymentProviderExchangeStatus.Started)
            throw new DomainRuleException("A completed provider exchange cannot be changed.");
        if (!Enum.IsDefined(outcome))
            throw new DomainRuleException("Provider exchange outcome is invalid.");
        if (completedAt < StartedAt)
            throw new DomainRuleException("Provider exchange completion cannot predate its start.");
        if (httpStatusCode is < 100 or > 599)
            throw new DomainRuleException("Provider exchange HTTP status code is invalid.");

        Outcome = outcome;
        CompletedAt = completedAt;
        RequestPayloadJson = NormalizeJson(requestPayloadJson) ?? RequestPayloadJson;
        ResponsePayloadJson = NormalizeJson(responsePayloadJson);
        HttpStatusCode = httpStatusCode;
        FailureCode = Normalize(failureCode, 100);
        FailureMessage = NormalizeFailureMessage(failureMessage);
        Status = PaymentProviderExchangeStatus.Completed;
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new DomainRuleException($"Provider exchange field exceeds {maxLength} characters.");
        return normalized;
    }

    private static string? NormalizeFailureMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= 500 ? normalized : normalized[..500];
    }

    private static string? NormalizeJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (System.Text.Encoding.UTF8.GetByteCount(value) > 256 * 1024)
            throw new DomainRuleException("Provider exchange evidence exceeds the maximum size.");

        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(value);
            return value;
        }
        catch (System.Text.Json.JsonException)
        {
            throw new DomainRuleException("Provider exchange evidence must be valid JSON.");
        }
    }
}
