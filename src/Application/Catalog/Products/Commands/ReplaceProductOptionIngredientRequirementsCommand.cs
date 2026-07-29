using Application.Catalog.Products.Requests;

namespace Application.Catalog.Products.Commands;

public sealed class ReplaceProductOptionIngredientRequirementsCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public long OptionGroupId { get; init; }
    public Guid ProductOptionId { get; init; }
    public required ReplaceProductOptionIngredientRequirementsRequest Request { get; init; }
    public Guid? UpdatedByAccountId { get; init; }
}
