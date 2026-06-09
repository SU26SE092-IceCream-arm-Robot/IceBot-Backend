using Application.SalesCatalog.Menus.Requests;
using System;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class UpdateMenuCommand
{
    public Guid MenuId { get; init; }
    public UpdateMenuRequest Request { get; init; } = null!;
    public Guid? UpdatedByAccountId { get; init; }
}
