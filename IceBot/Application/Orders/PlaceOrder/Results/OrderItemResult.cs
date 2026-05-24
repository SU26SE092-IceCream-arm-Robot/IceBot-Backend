using Domain.Orders.Enums;

namespace Application.Orders.PlaceOrder.Results;

public sealed class OrderItemResult
{
    public Guid Id { get; set; }

    public Guid MenuItemId { get; set; }

    public Guid ProductId { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid? RecipeId { get; set; }

    public string? ClientLineId { get; set; }

    public string MenuItemCodeSnapshot { get; set; } = null!;

    public string MenuItemNameSnapshot { get; set; } = null!;

    public string ProductCodeSnapshot { get; set; } = null!;

    public string ProductNameSnapshot { get; set; } = null!;

    public string ProductVariantCodeSnapshot { get; set; } = null!;

    public string ProductVariantNameSnapshot { get; set; } = null!;

    public int? RecipeVersionSnapshot { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public OrderItemStatus Status { get; set; }
}
