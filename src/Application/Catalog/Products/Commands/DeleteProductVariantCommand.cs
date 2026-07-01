namespace Application.Catalog.Products.Commands;

public sealed class DeleteProductVariantCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public Guid VariantId { get; init; }
    public Guid? DeletedByAccountId { get; init; }
}
