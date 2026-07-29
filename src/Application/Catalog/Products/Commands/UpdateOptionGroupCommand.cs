using Application.Catalog.Products.Requests;

namespace Application.Catalog.Products.Commands;

public sealed class UpdateOptionGroupCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public long OptionGroupId { get; init; }
    public required UpdateOptionGroupRequest Request { get; init; }
    public Guid? UpdatedByAccountId { get; init; }
}
