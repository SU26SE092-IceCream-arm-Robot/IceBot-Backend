namespace Application.SalesCatalog.Menus.Commands;

public sealed class DeleteMenuItemCommand
{
    public required MenuManagementCommandScope Scope { get; init; }
    public Guid MenuId { get; init; }
    public Guid MenuItemId { get; init; }
    public Guid? DeletedByAccountId { get; init; }
}
