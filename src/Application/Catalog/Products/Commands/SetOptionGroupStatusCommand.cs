namespace Application.Catalog.Products.Commands;

public sealed class SetOptionGroupStatusCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public long OptionGroupId { get; init; }
    public bool IsActive { get; init; }
    public Guid? UpdatedByAccountId { get; init; }
}
