using Application.SalesCatalog.Menus.Requests;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class AddMenuItemCommand
{
    public required MenuManagementCommandScope Scope { get; init; }
    public Guid MenuId { get; init; }
    public CreateMenuItemRequest Request { get; init; } = null!;
    public Guid? CreatedByAccountId { get; init; }
}
