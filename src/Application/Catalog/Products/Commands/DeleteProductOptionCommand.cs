namespace Application.Catalog.Products.Commands;

public sealed class DeleteProductOptionCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public long OptionGroupId { get; init; }
    public Guid ProductOptionId { get; init; }
}
