using Application.Payments.Abstractions;
using Application.Payments.Providers;
using Application.Shared.Exceptions;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Infrastructure.Payments.Options;
using Infrastructure.Payments.Observability;
using Infrastructure.Payments.Providers.PayOS.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Infrastructure.Payments.Providers.PayOS;

public sealed class PayOsPaymentGateway : IPaymentGateway
{
    private const int PayOsDescriptionMaxLength = 25;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PayOsOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayOsPaymentGateway> _logger;

    public PayOsPaymentGateway(
        IOptions<PayOsOptions> options,
        HttpClient httpClient,
        ILogger<PayOsPaymentGateway> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public string ProviderCode => "PayOS";

    public string CreateProviderOrderCode(Guid paymentTransactionId) =>
        GenerateOrderCode(paymentTransactionId).ToString(CultureInfo.InvariantCulture);

    public async Task<ProviderPaymentSession> CreatePaymentSessionAsync(
        PaymentTransaction paymentTransaction,
        Order order,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var amount = decimal.ToInt32(decimal.Round(paymentTransaction.Amount, 0, MidpointRounding.AwayFromZero));
        if (!long.TryParse(paymentTransaction.ProviderOrderCode, NumberStyles.None, CultureInfo.InvariantCulture, out var orderCode) ||
            orderCode <= 0)
        {
            throw new InvalidOperationException("Payment transaction provider order code is invalid.");
        }
        var description = SanitizeDescription($"IceBot #{orderCode}");
        var providerExpiresAt = _options.ExpireMinutes > 0
            ? DateTimeOffset.UtcNow.AddMinutes(_options.ExpireMinutes)
            : (DateTimeOffset?)null;
        var orderDeadlineAt = order.PaymentDeadlineAt == default
            ? (DateTimeOffset?)null
            : order.PaymentDeadlineAt;
        var expiresAt = providerExpiresAt.HasValue && orderDeadlineAt.HasValue
            ? (providerExpiresAt.Value <= orderDeadlineAt.Value ? providerExpiresAt : orderDeadlineAt)
            : providerExpiresAt ?? orderDeadlineAt;

        var request = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            CancelUrl = _options.CancelUrl,
            ReturnUrl = _options.ReturnUrl,
            ExpiredAt = expiresAt?.ToUnixTimeSeconds(),
            Items =
            {
                new PaymentItem
                {
                    Name = description,
                    Quantity = 1,
                    Price = amount
                }
            }
        };

        request.Signature = CreatePaymentLinkSignature(request);

        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("v2/payment-requests", content, cancellationToken);
        }
        catch (TimeoutRejectedException)
        {
            PayOsResilienceMetrics.RecordTimeout();
            throw new ProviderPaymentSessionCreationException(
                "PayOS payment-session creation timed out.",
                outcomeUnknown: true);
        }
        catch (BrokenCircuitException)
        {
            PayOsResilienceMetrics.RecordCircuitOpen();
            throw new ProviderPaymentSessionCreationException(
                "PayOS payment-session creation was blocked by the open circuit.",
                outcomeUnknown: false);
        }
        catch (HttpRequestException ex)
        {
            PayOsResilienceMetrics.RecordTransientFailure();
            throw new ProviderPaymentSessionCreationException(
                "PayOS payment-session creation failed at transport level.",
                outcomeUnknown: true,
                ex);
        }

        using (response)
        {
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode is 408 or 429 or >= 500)
                {
                    PayOsResilienceMetrics.RecordTransientFailure();
                }

                _logger.LogError("PayOS create payment link failed. Status={StatusCode}, Body={Body}", response.StatusCode, responseJson);
                throw new ProviderPaymentSessionCreationException(
                    "PayOS create payment link failed.",
                    outcomeUnknown: (int)response.StatusCode is 408 or 429 or >= 500);
            }

            var apiResponse = JsonSerializer.Deserialize<PayOsApiResponse<PaymentLinkData>>(responseJson, JsonOptions);
            if (apiResponse?.Code != "00" || apiResponse.Data is null)
            {
                var message = apiResponse?.Description ?? "Invalid PayOS response.";
                _logger.LogError("PayOS create payment link returned error: {Message}. Body={Body}", message, responseJson);
                throw new ProviderPaymentSessionCreationException(message, outcomeUnknown: false);
            }

            if (apiResponse.Data.OrderCode != orderCode ||
                (string.IsNullOrWhiteSpace(apiResponse.Data.CheckoutUrl) &&
                 string.IsNullOrWhiteSpace(apiResponse.Data.QrCode)))
            {
                _logger.LogError("PayOS create payment link returned incomplete data. Body={Body}", responseJson);
                throw new ProviderPaymentSessionCreationException(
                    "PayOS returned an invalid or incomplete payment session.",
                    outcomeUnknown: true);
            }

            return new ProviderPaymentSession
            {
                ProviderOrderCode = apiResponse.Data.OrderCode.ToString(CultureInfo.InvariantCulture),
                ProviderPaymentLinkId = apiResponse.Data.PaymentLinkId,
                CheckoutUrl = apiResponse.Data.CheckoutUrl,
                QrCodePayload = apiResponse.Data.QrCode,
                ExpiresAt = expiresAt,
                ProviderStatus = apiResponse.Data.Status,
                RawResponseJson = responseJson
            };
        }
    }

    public async Task<ProviderPaymentSession?> GetPaymentSessionAsync(
        string providerOrderCode,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        if (!long.TryParse(providerOrderCode, NumberStyles.None, CultureInfo.InvariantCulture, out var expectedOrderCode) ||
            expectedOrderCode <= 0)
        {
            throw new InvalidOperationException("Payment transaction provider order code is invalid.");
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(
                $"v2/payment-requests/{Uri.EscapeDataString(providerOrderCode)}",
                cancellationToken);
        }
        catch (TimeoutRejectedException)
        {
            PayOsResilienceMetrics.RecordTimeout("get_payment_session");
            throw;
        }
        catch (BrokenCircuitException)
        {
            PayOsResilienceMetrics.RecordCircuitOpen("get_payment_session");
            throw;
        }
        catch (HttpRequestException)
        {
            PayOsResilienceMetrics.RecordTransientFailure("get_payment_session");
            throw;
        }

        using (response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode is 408 or 429 or >= 500)
                {
                    PayOsResilienceMetrics.RecordTransientFailure("get_payment_session");
                }

                throw new InvalidOperationException("PayOS get payment link failed.");
            }

            var apiResponse = JsonSerializer.Deserialize<PayOsApiResponse<PaymentLinkInformationData>>(
                responseJson,
                JsonOptions);
            if (apiResponse?.Code != "00" || apiResponse.Data is null ||
                apiResponse.Data.OrderCode != expectedOrderCode)
            {
                throw new InvalidOperationException("PayOS returned invalid payment-link information.");
            }

            return new ProviderPaymentSession
            {
                ProviderOrderCode = apiResponse.Data.OrderCode.ToString(CultureInfo.InvariantCulture),
                ProviderPaymentLinkId = apiResponse.Data.PaymentLinkId,
                CheckoutUrl = apiResponse.Data.CheckoutUrl,
                QrCodePayload = apiResponse.Data.QrCode,
                ProviderStatus = apiResponse.Data.Status,
                Amount = apiResponse.Data.Amount,
                PaidAmount = apiResponse.Data.AmountPaid,
                RawResponseJson = responseJson
            };
        }
    }

    public Task<ProviderPaymentNotification> ParseAndVerifyNotificationAsync(
        string rawPayload,
        string? signature,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConfigured(requireApiCredentials: false);

        using var document = JsonDocument.Parse(rawPayload);
        var root = document.RootElement;
        var data = root.TryGetProperty("data", out var dataElement) ? dataElement : root;
        var payloadSignature = signature;

        if (string.IsNullOrWhiteSpace(payloadSignature) &&
            root.TryGetProperty("signature", out var signatureElement))
        {
            payloadSignature = signatureElement.GetString();
        }

        if (string.IsNullOrWhiteSpace(payloadSignature))
        {
            throw new InvalidOperationException("Missing PayOS webhook signature.");
        }

        var expectedSignature = CreateDataSignature(data);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(payloadSignature)))
        {
            throw new InvalidOperationException("Invalid PayOS webhook signature.");
        }

        var status = GetString(data, "status") ?? GetString(root, "status") ?? GetString(root, "code") ?? "UNKNOWN";
        var orderCode = GetString(data, "orderCode") ?? GetString(root, "orderCode");
        var paymentLinkId = GetString(data, "paymentLinkId") ?? GetString(root, "paymentLinkId");
        var reference = GetString(data, "reference") ?? GetString(data, "id") ?? GetString(root, "id");
        var amount = GetDecimal(data, "amount");
        var paidAt = GetDateTimeOffset(data, "transactionDateTime") ?? GetDateTimeOffset(root, "transactionDateTime");
        var providerEventId = GetString(data, "eventId") ?? GetString(root, "eventId");
        var eventId = !string.IsNullOrWhiteSpace(providerEventId)
            ? $"event:{providerEventId}"
            : $"payload:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawPayload)))}";
        var isPaid = IsPaidStatus(status);
        var isCancelled = IsCancelledStatus(status);
        var isExpired = IsExpiredStatus(status);

        return Task.FromResult(new ProviderPaymentNotification
        {
            Provider = ProviderCode,
            EventType = status,
            ProviderEventId = eventId,
            ProviderOrderCode = orderCode,
            ProviderPaymentLinkId = paymentLinkId,
            ProviderTransactionId = reference,
            ProviderStatus = status,
            IsPaid = isPaid,
            IsCancelled = isCancelled,
            IsExpired = isExpired,
            PaidAmount = isPaid ? amount : null,
            ProviderPaidAt = paidAt,
            RawPayloadJson = rawPayload
        });
    }

    private string CreatePaymentLinkSignature(CreatePaymentLinkRequest request)
    {
        var data = $"amount={request.Amount}" +
                   $"&cancelUrl={request.CancelUrl}" +
                   $"&description={request.Description}" +
                   $"&orderCode={request.OrderCode}" +
                   $"&returnUrl={request.ReturnUrl}";

        return CreateHmac(data);
    }

    private string CreateDataSignature(JsonElement data)
    {
        var flattened = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in data.EnumerateObject())
        {
            if (property.NameEquals("signature"))
            {
                continue;
            }

            flattened[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => property.Value.GetRawText()
            };
        }

        var dataToSign = string.Join("&", flattened.Select(pair => $"{pair.Key}={pair.Value}"));
        return CreateHmac(dataToSign);
    }

    private string CreateHmac(string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.ChecksumKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void EnsureConfigured(bool requireApiCredentials = true)
    {
        if (string.IsNullOrWhiteSpace(_options.ChecksumKey))
        {
            throw new AppException("PayOS integration is not configured.", 503);
        }

        if (!requireApiCredentials)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.ReturnUrl) ||
            string.IsNullOrWhiteSpace(_options.CancelUrl))
        {
            throw new AppException("PayOS integration is not configured.", 503);
        }
    }

    private static long GenerateOrderCode(Guid paymentTransactionId)
    {
        var bytes = paymentTransactionId.ToByteArray();
        var value = BitConverter.ToUInt64(bytes, 0) & long.MaxValue;
        return (long)(value % 9_000_000_000_000UL) + 1_000_000_000_000;
    }

    private static string SanitizeDescription(string input)
    {
        var sanitized = string.IsNullOrWhiteSpace(input) ? "IceBot payment" : input.Trim();
        return sanitized.Length <= PayOsDescriptionMaxLength
            ? sanitized
            : sanitized[..PayOsDescriptionMaxLength];
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.GetRawText();
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var value))
        {
            return value;
        }

        return decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static DateTimeOffset? GetDateTimeOffset(JsonElement element, string propertyName)
    {
        var value = GetString(element, propertyName);
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed
            : null;
    }

    private static bool IsPaidStatus(string status)
    {
        return status.Equals("PAID", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("00", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCancelledStatus(string status)
    {
        return status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("CANCELED", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("CANCEL", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExpiredStatus(string status)
    {
        return status.Equals("EXPIRED", StringComparison.OrdinalIgnoreCase);
    }
}
