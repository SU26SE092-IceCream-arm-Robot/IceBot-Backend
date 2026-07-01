using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class SetMenuItemStatusCommand
{
    public required MenuManagementCommandScope Scope { get; init; }
    public Guid MenuId { get; init; }
    public Guid MenuItemId { get; init; }
    public MenuItemStatus Status { get; init; }
    public Guid? UpdatedByAccountId { get; init; }
}
