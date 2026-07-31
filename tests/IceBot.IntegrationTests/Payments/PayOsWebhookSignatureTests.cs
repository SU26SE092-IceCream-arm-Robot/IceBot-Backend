using System.Security.Cryptography;
using System.Text;
using Infrastructure.Payments.Options;
using Infrastructure.Payments.Providers.PayOS;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.Payments;

public sealed class PayOsWebhookSignatureTests
{
    [Fact]
    public async Task ValidSignature_ParsesProviderNotificationWithoutCallingPayOsApi()
    {
        const string checksumKey = "test-checksum-key";
        const string orderCode = "9999999999999";
        const string dataToSign = "amount=30000&orderCode=9999999999999&status=PAID";
        var payload = $"{{\"data\":{{\"orderCode\":\"{orderCode}\",\"amount\":30000,\"status\":\"PAID\"}}}}";
        var gateway = CreateGateway(checksumKey);

        var notification = await gateway.ParseAndVerifyNotificationAsync(
            payload,
            CreateSignature(checksumKey, dataToSign));

        Assert.Equal("PayOS", notification.Provider);
        Assert.Equal(orderCode, notification.ProviderOrderCode);
        Assert.True(notification.IsPaid);
        Assert.Equal(30_000, notification.PaidAmount);
    }

    [Fact]
    public async Task InvalidSignature_IsRejected()
    {
        var gateway = CreateGateway("test-checksum-key");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gateway.ParseAndVerifyNotificationAsync(
                "{\"data\":{\"orderCode\":\"9999999999999\",\"amount\":30000,\"status\":\"PAID\"}}",
                "not-a-valid-signature"));

        Assert.Contains("Invalid PayOS webhook signature", exception.Message, StringComparison.Ordinal);
    }

    private static PayOsPaymentGateway CreateGateway(string checksumKey) => new(
        Options.Create(new PayOsOptions
        {
            ClientId = "client",
            ApiKey = "api-key",
            ChecksumKey = checksumKey,
            ReturnUrl = "https://icebot.test/return",
            CancelUrl = "https://icebot.test/cancel"
        }),
        new HttpClient(),
        NullLogger<PayOsPaymentGateway>.Instance);

    private static string CreateSignature(string checksumKey, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    }
}
