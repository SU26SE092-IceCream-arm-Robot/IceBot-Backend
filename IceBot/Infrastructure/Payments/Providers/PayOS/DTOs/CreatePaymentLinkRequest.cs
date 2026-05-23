using System.Text.Json.Serialization;

namespace Infrastructure.Payments.Providers.PayOS.DTOs;

/// <summary>
/// Request để tạo payment link PayOS
/// </summary>
public class CreatePaymentLinkRequest
{
    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("buyerName")]
    public string? BuyerName { get; set; }

    [JsonPropertyName("buyerEmail")]
    public string? BuyerEmail { get; set; }

    [JsonPropertyName("buyerPhone")]
    public string? BuyerPhone { get; set; }

    [JsonPropertyName("buyerAddress")]
    public string? BuyerAddress { get; set; }

    [JsonPropertyName("items")]
    public List<PaymentItem> Items { get; set; } = new();

    [JsonPropertyName("cancelUrl")]
    public required string CancelUrl { get; set; }

    [JsonPropertyName("returnUrl")]
    public required string ReturnUrl { get; set; }

    [JsonPropertyName("expiredAt")]
    public long? ExpiredAt { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

public class PaymentItem
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public int Price { get; set; }
}
