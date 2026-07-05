namespace Application.Catalog.Products.Commands;

public sealed class SetProductOptionAvailabilityCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public long OptionGroupId { get; init; }
    public Guid ProductOptionId { get; init; }
    public bool IsAvailable { get; init; }
    public Guid? UpdatedByAccountId { get; init; }
}
