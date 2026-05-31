using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Menus.Requests;

public sealed class SetMenuStatusRequest
{
    public MenuStatus Status { get; set; }
}
