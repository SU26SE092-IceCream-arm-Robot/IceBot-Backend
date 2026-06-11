using Application.SalesCatalog.Menus.Requests;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class AddMenuItemCommand
{
    public Guid MenuId { get; init; }
    public CreateMenuItemRequest Request { get; init; } = null!;
    public Guid? CreatedByAccountId { get; init; }
}
