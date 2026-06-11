namespace Application.SalesCatalog.Menus.Commands;

public sealed class DeleteMenuCommand
{
    public Guid MenuId { get; init; }
    public Guid? DeletedByAccountId { get; init; }
}
