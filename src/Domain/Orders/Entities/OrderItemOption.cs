using Domain.Common;
using Domain.Catalog.Enums;

namespace Domain.Orders.Entities;

public class OrderItemOption : BusinessEntity
{
    public Guid OrderItemId { get; set; }

    public Guid ProductOptionId { get; set; }

    public long OptionGroupId { get; set; }

    public string OptionGroupCodeSnapshot { get; set; } = null!;

    public string CodeSnapshot { get; set; } = null!;

    public string NameSnapshot { get; set; } = null!;

    public decimal UnitPriceDelta { get; set; }

    public ProductOptionExecutionImpact ExecutionImpact { get; set; }

    public int Quantity { get; set; } = 1;

    public decimal TotalPriceDelta { get; set; }

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual ICollection<OrderItemOptionIngredientRequirement> IngredientRequirements { get; set; } =
        new List<OrderItemOptionIngredientRequirement>();

    public static OrderItemOption Create(
        Guid productOptionId,
        long optionGroupId,
        string optionGroupCode,
        string code,
        string name,
        decimal unitPriceDelta,
        ProductOptionExecutionImpact executionImpact)
    {
        if (productOptionId == Guid.Empty || optionGroupId <= 0)
        {
            throw new DomainRuleException("A valid product option and option group are required.");
        }

        if (string.IsNullOrWhiteSpace(optionGroupCode) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Option snapshot identity is required.");
        }

        if (unitPriceDelta < 0)
        {
            throw new DomainRuleException("Option price delta cannot be negative.");
        }

        return new OrderItemOption
        {
            ProductOptionId = productOptionId,
            OptionGroupId = optionGroupId,
            OptionGroupCodeSnapshot = optionGroupCode.Trim(),
            CodeSnapshot = code.Trim(),
            NameSnapshot = name.Trim(),
            UnitPriceDelta = unitPriceDelta,
            ExecutionImpact = executionImpact,
            Quantity = 1,
            TotalPriceDelta = unitPriceDelta
        };
    }
}
