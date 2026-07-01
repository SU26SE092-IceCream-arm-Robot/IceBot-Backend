using Application.Catalog.Products.Requests;

namespace Application.Catalog.Products.Commands;

public sealed class AddProductVariantCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public UpsertProductVariantRequest Request { get; init; } = null!;
    public Guid? CreatedByAccountId { get; init; }
}
