using Application.SalesCatalog.Menus.Requests;
using System;

namespace Application.SalesCatalog.Menus.Commands;

public sealed class CreateMenuCommand
{
    public CreateMenuRequest Request { get; init; } = null!;
    public Guid? CreatedByAccountId { get; init; }
}
