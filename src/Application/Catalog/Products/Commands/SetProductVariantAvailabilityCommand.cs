namespace Application.Catalog.Products.Commands;

public sealed class SetProductVariantAvailabilityCommand
{
    public Guid ProductId { get; init; }
    public Guid VariantId { get; init; }
    public bool IsAvailable { get; init; }
    public Guid? UpdatedByAccountId { get; init; }
}
