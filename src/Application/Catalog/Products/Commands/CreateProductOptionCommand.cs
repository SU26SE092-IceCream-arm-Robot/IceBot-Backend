using Application.Catalog.Products.Requests;

namespace Application.Catalog.Products.Commands;

public sealed class CreateProductOptionCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public long OptionGroupId { get; init; }
    public required CreateProductOptionRequest Request { get; init; }
    public Guid? CreatedByAccountId { get; init; }
}
