using Application.Catalog.Products.Requests;

namespace Application.Catalog.Products.Commands;

public sealed class CreateOptionGroupCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public required CreateOptionGroupRequest Request { get; init; }
    public Guid? CreatedByAccountId { get; init; }
}
