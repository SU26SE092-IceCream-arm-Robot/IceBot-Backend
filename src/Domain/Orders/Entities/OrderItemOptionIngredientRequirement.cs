using Domain.Common;

namespace Domain.Orders.Entities;

/// <summary>Order-time snapshot of an ingredient adjustment selected by an option.</summary>
public sealed class OrderItemOptionIngredientRequirement : BusinessEntity
{
    public Guid OrderItemOptionId { get; set; }
    public Guid IngredientId { get; set; }
    public string IngredientCodeSnapshot { get; set; } = null!;
    public string IngredientNameSnapshot { get; set; } = null!;
    public decimal QuantityPerOption { get; set; }
    public string Unit { get; set; } = null!;
    public string RequiredWorkcellCapabilityCode { get; set; } = null!;

    public OrderItemOption OrderItemOption { get; set; } = null!;
}
