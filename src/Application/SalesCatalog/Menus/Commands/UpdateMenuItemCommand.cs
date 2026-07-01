using Application.SalesCatalog.Menus.Requests;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class UpdateMenuItemCommand
{
    public required MenuManagementCommandScope Scope { get; init; }
    public Guid MenuId { get; init; }
    public Guid MenuItemId { get; init; }
    public UpdateMenuItemRequest Request { get; init; } = null!;
    public Guid? UpdatedByAccountId { get; init; }
}
