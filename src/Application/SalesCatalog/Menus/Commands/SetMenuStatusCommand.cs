using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class SetMenuStatusCommand
{
    public Guid MenuId { get; init; }
    public MenuStatus Status { get; init; }
    public Guid? UpdatedByAccountId { get; init; }
}
