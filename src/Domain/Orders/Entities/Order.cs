using Domain.Common;
using Domain.Orders.Enums;
using Domain.Tenants.Entities;

namespace Domain.Orders.Entities;

public partial class Order : BusinessEntity, IStoreScoped
{
    public Guid? OrganizationId { get; set; }

    public Guid KioskId { get; set; }

    public Guid? StoreId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public string? IdempotencyKey { get; set; }

    public string? ClientOrderId { get; set; }

    public Guid? CorrelationId { get; set; }

    public Guid? RuntimeSnapshotId { get; set; }

    public DateTimeOffset? RuntimeSnapshotGeneratedAt { get; set; }

    public OrderChannel Channel { get; set; } = OrderChannel.Tablet;

    public string? ExternalChannel { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    public string Currency { get; set; } = "VND";

    public decimal SubtotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerPhoneNumber { get; set; }

    public DateTimeOffset PlacedAt { get; set; }

    public DateTimeOffset? PaidAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public string? Notes { get; set; }

    public virtual Kiosk Kiosk { get; set; } = null!;

    public virtual Organization? Organization { get; set; }

    public virtual Store? Store { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public OrderItem AddItem(
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
        string? optionsJson = null,
        string? recipeSnapshotJson = null)
    {
        EnsureEditable();

        if (!string.IsNullOrWhiteSpace(clientLineId) &&
            OrderItems.Any(item => string.Equals(item.ClientLineId, clientLineId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainRuleException("An order item with the same client line id already exists.");
        }

        var orderItem = OrderItem.Create(
            menuItemId,
            productId,
            productVariantId,
            recipeId,
            menuItemCodeSnapshot,
            menuItemNameSnapshot,
            productCodeSnapshot,
            productNameSnapshot,
            productVariantCodeSnapshot,
            productVariantNameSnapshot,
            recipeVersionSnapshot,
            quantity,
            unitPrice,
            discountAmount,
            clientLineId,
            optionsJson,
            recipeSnapshotJson);

        OrderItems.Add(orderItem);
        RecalculateTotals();

        return orderItem;
    }

    public void RecalculateTotals()
    {
        foreach (var item in OrderItems)
        {
            item.RecalculateTotal();
        }

        SubtotalAmount = OrderItems.Sum(item => item.UnitPrice * item.Quantity);
        DiscountAmount = OrderItems.Sum(item => item.DiscountAmount);

        if (TaxAmount < 0)
        {
            throw new DomainRuleException("Order tax cannot be negative.");
        }

        TotalAmount = SubtotalAmount - DiscountAmount + TaxAmount;

        if (TotalAmount < 0)
        {
            throw new DomainRuleException("Order total cannot be negative.");
        }
    }

    public void Place(DateTimeOffset placedAt)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainRuleException("Only draft orders can be placed.");
        }

        if (!OrderItems.Any())
        {
            throw new DomainRuleException("Cannot place an order without items.");
        }

        RecalculateTotals();
        PlacedAt = placedAt;
        Status = OrderStatus.PendingPayment;
    }

    public void MarkPaid(decimal paidAmount, DateTimeOffset paidAt)
    {
        if (paidAmount <= 0)
        {
            throw new DomainRuleException("Paid amount must be greater than zero.");
        }

        PaidAmount += paidAmount;
        PaidAt = paidAt;

        PaymentStatus = PaidAmount >= TotalAmount
            ? PaymentStatus.Paid
            : PaymentStatus.Authorized;

        if (PaymentStatus == PaymentStatus.Paid && Status == OrderStatus.PendingPayment)
        {
            Status = OrderStatus.ReadyForExecution;
        }
    }

    public void MarkExecutionRejected(string? notes = null)
    {
        if (Status is not (OrderStatus.Paid or OrderStatus.ReadyForExecution or OrderStatus.Accepted))
        {
            throw new DomainRuleException("Only paid or execution-ready orders can be rejected by execution.");
        }

        Status = OrderStatus.ExecutionRejected;
        Notes = notes ?? Notes;
    }

    public void MarkAccepted()
    {
        if (Status != OrderStatus.ReadyForExecution)
        {
            throw new DomainRuleException("Only execution-ready orders can be accepted by an executor.");
        }

        Status = OrderStatus.Accepted;
    }

    public void MarkRefundRequired(string? notes = null)
    {
        if (PaymentStatus != PaymentStatus.Paid)
        {
            throw new DomainRuleException("Only paid orders can require refund.");
        }

        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot require refund for completed or cancelled orders.");
        }

        Status = OrderStatus.RefundRequired;
        Notes = notes ?? Notes;
    }

    public void MarkPreparing()
    {
        if (Status is not (OrderStatus.ReadyForExecution or OrderStatus.Accepted))
        {
            throw new DomainRuleException("Only execution-ready or accepted orders can be prepared.");
        }

        Status = OrderStatus.Preparing;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status is OrderStatus.Cancelled or OrderStatus.Failed or OrderStatus.ExecutionRejected or OrderStatus.RefundRequired)
        {
            throw new DomainRuleException("Cannot complete a cancelled, failed, rejected, or refund-required order.");
        }

        Status = OrderStatus.Completed;
        CompletedAt = completedAt;
    }

    public void Cancel(DateTimeOffset cancelledAt, string? notes = null)
    {
        if (Status == OrderStatus.Completed)
        {
            throw new DomainRuleException("Cannot cancel a completed order.");
        }

        Status = OrderStatus.Cancelled;
        CancelledAt = cancelledAt;
        Notes = notes ?? Notes;
    }

    public void MarkRefunded()
    {
        if (Status != OrderStatus.RefundRequired)
        {
            throw new DomainRuleException("Only refund-required orders can be marked as refunded.");
        }
        Status = OrderStatus.Refunded;
    }

    public void MarkCompensated()
    {
        if (Status != OrderStatus.RefundRequired)
        {
            throw new DomainRuleException("Only refund-required orders can be marked as compensated.");
        }
        Status = OrderStatus.Compensated;
    }

    private void EnsureEditable()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainRuleException("Only draft orders can be edited.");
        }
    }
}
