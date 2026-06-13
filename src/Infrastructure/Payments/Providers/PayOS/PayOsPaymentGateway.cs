using Application.Payments.Abstractions;
using Application.Payments.Providers;
using Application.Shared.Exceptions;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Infrastructure.Payments.Options;
using Infrastructure.Payments.Providers.PayOS.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Payments.Providers.PayOS;

public sealed class PayOsPaymentGateway : IPaymentGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PayOsOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PayOsPaymentGateway> _logger;

    public PayOsPaymentGateway(
        IOptions<PayOsOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<PayOsPaymentGateway> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string ProviderCode => "PayOS";

    public async Task<ProviderPaymentSession> CreatePaymentSessionAsync(
        PaymentTransaction paymentTransaction,
        Order order,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var amount = decimal.ToInt32(decimal.Round(paymentTransaction.Amount, 0, MidpointRounding.AwayFromZero));
        var orderCode = GenerateOrderCode(paymentTransaction.Id);
        var description = SanitizeDescription($"IceBot {order.OrderNumber}");
        var expiresAt = _options.ExpireMinutes > 0
            ? DateTimeOffset.UtcNow.AddMinutes(_options.ExpireMinutes)
            : (DateTimeOffset?)null;

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

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("x-client-id", _options.ClientId);
        httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);

        var url = $"{_options.BaseUrl.TrimEnd('/')}/v2/payment-requests";
        var response = await httpClient.PostAsync(url, content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayOS create payment link failed. Status={StatusCode}, Body={Body}", response.StatusCode, responseJson);
            throw new InvalidOperationException("PayOS create payment link failed.");
        }

        var apiResponse = JsonSerializer.Deserialize<PayOsApiResponse<PaymentLinkData>>(responseJson, JsonOptions);
        if (apiResponse?.Code != "00" || apiResponse.Data is null)
        {
            var message = apiResponse?.Description ?? "Invalid PayOS response.";
            _logger.LogError("PayOS create payment link returned error: {Message}. Body={Body}", message, responseJson);
            throw new InvalidOperationException(message);
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
        var eventId = reference ?? paymentLinkId ?? orderCode;
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
        return sanitized.Length <= 255 ? sanitized : sanitized[..255];
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
