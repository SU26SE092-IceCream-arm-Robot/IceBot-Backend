using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Menus.Requests;

public sealed class SetMenuItemStatusRequest
{
    public MenuItemStatus Status { get; set; }
}
