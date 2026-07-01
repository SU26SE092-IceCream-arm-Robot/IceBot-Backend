using Application.Catalog.Products.Requests;

namespace Application.Catalog.Products.Commands;

public sealed class UpdateProductVariantCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public Guid VariantId { get; init; }
    public UpdateProductVariantRequest Request { get; init; } = null!;
    public Guid? UpdatedByAccountId { get; init; }
}
