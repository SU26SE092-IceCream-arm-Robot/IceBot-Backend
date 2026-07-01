namespace Application.Catalog.Products.Commands;

public sealed class DeleteProductCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public Guid? DeletedByAccountId { get; init; }
}
