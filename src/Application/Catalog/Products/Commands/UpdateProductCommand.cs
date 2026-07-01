using Application.Catalog.Products.Requests;

namespace Application.Catalog.Products.Commands;

public sealed class UpdateProductCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public UpdateProductRequest Request { get; init; } = null!;
    public Guid? UpdatedByAccountId { get; init; }
}
