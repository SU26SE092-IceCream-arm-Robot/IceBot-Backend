using Domain.Orders.Enums;

namespace Application.Orders.Management.Results;

public sealed class ManagementOrderListItemResult
{
    public Guid Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid KioskId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string? ClientOrderId { get; init; }
    public OrderStatus Status { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public string Currency { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhoneNumber { get; init; }
    public DateTimeOffset PlacedAt { get; init; }
    public DateTimeOffset? PaidAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string CustomerStatus { get; init; } = null!;
    public bool CanRetryPayment { get; init; }
    public bool RequiresStaffSupport { get; init; }
}

public sealed class ManagementOrderDetailResult
{
    public Guid Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid KioskId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string? ClientOrderId { get; init; }
    public OrderChannel Channel { get; init; }
    public string? ExternalChannel { get; init; }
    public OrderStatus Status { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public string Currency { get; init; } = null!;
    public decimal SubtotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhoneNumber { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset PlacedAt { get; init; }
    public DateTimeOffset? PaidAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string CustomerStatus { get; init; } = null!;
    public string CustomerStatusMessage { get; init; } = null!;
    public bool CanRetryPayment { get; init; }
    public bool RequiresStaffSupport { get; init; }
    public IReadOnlyCollection<ManagementOrderItemResult> Items { get; init; } = [];
}

public sealed class ManagementOrderItemResult
{
    public Guid Id { get; init; }
    public Guid MenuItemId { get; init; }
    public Guid ProductId { get; init; }
    public Guid ProductVariantId { get; init; }
    public Guid? RecipeId { get; init; }
    public string? ClientLineId { get; init; }
    public string MenuItemCode { get; init; } = null!;
    public string MenuItemName { get; init; } = null!;
    public string ProductCode { get; init; } = null!;
    public string ProductName { get; init; } = null!;
    public string ProductVariantCode { get; init; } = null!;
    public string ProductVariantName { get; init; } = null!;
    public int? RecipeVersion { get; init; }
    public Domain.Catalog.Enums.FulfillmentType FulfillmentType { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public OrderItemStatus Status { get; init; }
    public IReadOnlyCollection<ManagementOrderItemOptionResult> SelectedOptions { get; init; } = [];
}

public sealed class ManagementOrderItemOptionResult
{
    public Guid ProductOptionId { get; init; }
    public string OptionGroupCode { get; init; } = null!;
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public decimal PriceDelta { get; init; }
}
