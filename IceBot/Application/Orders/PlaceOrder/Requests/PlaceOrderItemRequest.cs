namespace Application.Orders.PlaceOrder.Requests;

public sealed class PlaceOrderItemRequest
{
    public Guid ProductId { get; set; }

    public Guid? RecipeId { get; set; }

    public string? ClientLineId { get; set; }

    public int Quantity { get; set; }

    public string? OptionsJson { get; set; }
}
