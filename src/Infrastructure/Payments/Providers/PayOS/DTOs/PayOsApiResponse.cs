using System.Text.Json.Serialization;

namespace Infrastructure.Payments.Providers.PayOS.DTOs;

/// <summary>
/// Response từ PayOS API khi tạo payment link
/// </summary>
public class PayOsApiResponse<T>
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("desc")]
    public string? Description { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

public class PaymentLinkData
{
    [JsonPropertyName("bin")]
    public string? Bin { get; set; }

    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }

    [JsonPropertyName("accountName")]
    public string? AccountName { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("paymentLinkId")]
    public string? PaymentLinkId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("checkoutUrl")]
    public string? CheckoutUrl { get; set; }

    [JsonPropertyName("qrCode")]
    public string? QrCode { get; set; }
}

public sealed class PaymentLinkInformationData
{
    [JsonPropertyName("id")]
    public string? PaymentLinkId { get; set; }

    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("amountPaid")]
    public decimal AmountPaid { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("checkoutUrl")]
    public string? CheckoutUrl { get; set; }

    [JsonPropertyName("qrCode")]
    public string? QrCode { get; set; }
}
