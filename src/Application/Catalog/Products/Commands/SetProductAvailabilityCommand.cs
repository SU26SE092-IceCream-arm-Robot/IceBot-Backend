namespace Application.Catalog.Products.Commands;

public sealed class SetProductAvailabilityCommand
{
    public required ProductManagementCommandScope Scope { get; init; }
    public Guid ProductId { get; init; }
    public bool IsAvailable { get; init; }
    public Guid? UpdatedByAccountId { get; init; }
}
