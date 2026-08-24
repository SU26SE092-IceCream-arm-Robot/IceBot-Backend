using Application.Payments.Providers;
using Domain.Common;
using Domain.Payments.Entities;
using Domain.Payments.Enums;

namespace IceBot.UnitTests.Payments;

public sealed class PaymentProviderExchangeTests
{
    [Fact]
    public void StartedExchange_CompletesOnceAndPreservesItsRequestEvidence()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var exchange = PaymentProviderExchange.Start(
            Guid.NewGuid(), "PayOS", PaymentProviderExchangeOperation.CreateSession, 1, "123", startedAt);

        exchange.Complete(
            PaymentProviderExchangeOutcome.Succeeded,
            startedAt.AddSeconds(1),
            "{\"orderCode\":123}",
            "{\"code\":\"00\"}",
            200);

        Assert.Equal(PaymentProviderExchangeStatus.Completed, exchange.Status);
        Assert.Equal(PaymentProviderExchangeOutcome.Succeeded, exchange.Outcome);
        Assert.Equal("{\"orderCode\":123}", exchange.RequestPayloadJson);
        Assert.Throws<DomainRuleException>(() => exchange.Complete(
            PaymentProviderExchangeOutcome.Rejected,
            startedAt.AddSeconds(2), null, null, 400));
    }

    [Fact]
    public void Exchange_RejectsInvalidLifecycleEvidence()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var exchange = PaymentProviderExchange.Start(
            Guid.NewGuid(), "PayOS", PaymentProviderExchangeOperation.LookupSession, 1, "123", startedAt);

        Assert.Throws<DomainRuleException>(() => exchange.Complete(
            PaymentProviderExchangeOutcome.Succeeded,
            startedAt.AddSeconds(-1), null, null, 200));
        Assert.Throws<DomainRuleException>(() => exchange.Complete(
            PaymentProviderExchangeOutcome.Succeeded,
            startedAt.AddSeconds(1), null, null, 99));
    }

    [Fact]
    public void Exchange_BoundsProviderFailureMessages()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var exchange = PaymentProviderExchange.Start(
            Guid.NewGuid(), "PayOS", PaymentProviderExchangeOperation.CreateSession, 1, "123", startedAt);

        exchange.Complete(
            PaymentProviderExchangeOutcome.Rejected,
            startedAt.AddSeconds(1),
            null,
            null,
            400,
            "PROVIDER_REJECTED",
            new string('x', 501));

        Assert.Equal(500, exchange.FailureMessage!.Length);
    }

    [Fact]
    public void Evidence_RedactsSecretsAndBoundsOversizedPayloads()
    {
        var evidence = ProviderExchangeEvidence.FromRaw(
            "{\"signature\":\"secret\",\"buyer_email\":\"customer@example.com\",\"orderCode\":123}",
            "{\"token\":\"secret\",\"accountNumber\":\"0123456789\",\"accountName\":\"IceBot\",\"qrCode\":\"bank-payload\",\"status\":\"PENDING\"}",
            200);

        Assert.DoesNotContain("secret", evidence.RequestPayloadJson, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", evidence.RequestPayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("customer@example.com", evidence.RequestPayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", evidence.ResponsePayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("0123456789", evidence.ResponsePayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("bank-payload", evidence.ResponsePayloadJson, StringComparison.Ordinal);
        Assert.Equal(200, evidence.HttpStatusCode);
    }

    [Fact]
    public void Evidence_ReplacesOversizedPayloadWithBoundedDigestEnvelope()
    {
        var evidence = ProviderExchangeEvidence.FromRaw(
            "{\"description\":\"" + new string('x', ProviderExchangeEvidence.MaximumPayloadLength) + "\"}",
            null,
            null);

        Assert.NotNull(evidence.RequestPayloadJson);
        Assert.Contains("\"truncated\":true", evidence.RequestPayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"byteLength\":", evidence.RequestPayloadJson, StringComparison.Ordinal);
        Assert.Contains("\"sha256\":", evidence.RequestPayloadJson, StringComparison.Ordinal);
        Assert.True(evidence.RequestPayloadJson.Length < ProviderExchangeEvidence.MaximumPayloadLength);
    }
}
