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

    public string MenuItemCode { get; set; } = null!;

    public string MenuItemName { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string ProductVariantCode { get; set; } = null!;

    public string ProductVariantName { get; set; } = null!;

    public int? RecipeVersion { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public List<OrderItemOptionResult> SelectedOptions { get; set; } = new();

    public OrderItemStatus Status { get; set; }
}

public sealed class OrderItemOptionResult
{
    public Guid ProductOptionId { get; set; }
    public string OptionGroupCode { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal PriceDelta { get; set; }
}
