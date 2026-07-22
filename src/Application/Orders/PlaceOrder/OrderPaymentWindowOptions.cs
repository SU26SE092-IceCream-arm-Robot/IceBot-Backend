namespace Application.Orders.PlaceOrder;

public sealed class OrderPaymentWindowOptions
{
    public const string SectionName = "Payments:OrderPaymentWindow";

    public int DurationMinutes { get; set; } = 15;
}
