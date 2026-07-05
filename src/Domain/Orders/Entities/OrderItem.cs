using Domain.Catalog.Entities;
using Domain.Common;
using Domain.Orders.Enums;
using Domain.SalesCatalog.Entities;

namespace Domain.Orders.Entities;

public partial class OrderItem : BusinessEntity
{
    public Guid OrderId { get; set; }

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

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public OrderItemStatus Status { get; set; } = OrderItemStatus.Pending;

    public int RecipeSnapshotSchemaVersion { get; set; } = 1;

    public string? RecipeSnapshotJson { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual MenuItem MenuItem { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public virtual Recipe? Recipe { get; set; }

    public virtual ICollection<OrderItemOption> Options { get; set; } = new List<OrderItemOption>();

    public static OrderItem Create(
        Guid menuItemId,
        Guid productId,
        Guid productVariantId,
        Guid? recipeId,
        string menuItemCodeSnapshot,
        string menuItemNameSnapshot,
        string productCodeSnapshot,
        string productNameSnapshot,
        string productVariantCodeSnapshot,
        string productVariantNameSnapshot,
        int? recipeVersionSnapshot,
        int quantity,
        decimal unitPrice,
        decimal discountAmount = 0,
        string? clientLineId = null,
        string? recipeSnapshotJson = null)
    {
        if (menuItemId == Guid.Empty)
        {
            throw new DomainRuleException("Menu item is required for an order item.");
        }

        if (productId == Guid.Empty)
        {
            throw new DomainRuleException("Product is required for an order item.");
        }

        if (productVariantId == Guid.Empty)
        {
            throw new DomainRuleException("Product variant is required for an order item.");
        }

        if (string.IsNullOrWhiteSpace(productCodeSnapshot))
        {
            throw new DomainRuleException("Product code snapshot is required.");
        }

        if (string.IsNullOrWhiteSpace(menuItemCodeSnapshot))
        {
            throw new DomainRuleException("Menu item code snapshot is required.");
        }

        if (string.IsNullOrWhiteSpace(menuItemNameSnapshot))
        {
            throw new DomainRuleException("Menu item name snapshot is required.");
        }

        if (string.IsNullOrWhiteSpace(productNameSnapshot))
        {
            throw new DomainRuleException("Product name snapshot is required.");
        }

        if (string.IsNullOrWhiteSpace(productVariantCodeSnapshot))
        {
            throw new DomainRuleException("Product variant code snapshot is required.");
        }

        if (string.IsNullOrWhiteSpace(productVariantNameSnapshot))
        {
            throw new DomainRuleException("Product variant name snapshot is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleException("Order item quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainRuleException("Order item unit price cannot be negative.");
        }

        if (discountAmount < 0)
        {
            throw new DomainRuleException("Order item discount cannot be negative.");
        }

        var item = new OrderItem
        {
            MenuItemId = menuItemId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            RecipeId = recipeId,
            MenuItemCodeSnapshot = menuItemCodeSnapshot.Trim(),
            MenuItemNameSnapshot = menuItemNameSnapshot.Trim(),
            ProductCodeSnapshot = productCodeSnapshot.Trim(),
            ProductNameSnapshot = productNameSnapshot.Trim(),
            ProductVariantCodeSnapshot = productVariantCodeSnapshot.Trim(),
            ProductVariantNameSnapshot = productVariantNameSnapshot.Trim(),
            RecipeVersionSnapshot = recipeVersionSnapshot,
            ClientLineId = string.IsNullOrWhiteSpace(clientLineId) ? null : clientLineId.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountAmount = discountAmount,
            RecipeSnapshotJson = recipeSnapshotJson
        };

        item.RecalculateTotal();
        return item;
    }

    public void ChangeQuantity(int quantity)
    {
        if (Status is OrderItemStatus.Completed or OrderItemStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot change quantity after an order item is completed or cancelled.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleException("Order item quantity must be greater than zero.");
        }

        Quantity = quantity;
        RecalculateTotal();
    }

    public void RecalculateTotal()
    {
        var grossAmount = UnitPrice * Quantity;

        if (DiscountAmount > grossAmount)
        {
            throw new DomainRuleException("Order item discount cannot exceed gross amount.");
        }

        TotalAmount = grossAmount - DiscountAmount;
    }

    public void MarkPreparing()
    {
        if (Status is OrderItemStatus.Cancelled or OrderItemStatus.Failed)
        {
            throw new DomainRuleException("Cannot prepare a cancelled or failed order item.");
        }

        Status = OrderItemStatus.Preparing;
    }

    public void MarkCompleted()
    {
        if (Status == OrderItemStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot complete a cancelled order item.");
        }

        Status = OrderItemStatus.Completed;
    }

    public void Cancel()
    {
        if (Status == OrderItemStatus.Completed)
        {
            throw new DomainRuleException("Cannot cancel a completed order item.");
        }

        Status = OrderItemStatus.Cancelled;
    }
}
