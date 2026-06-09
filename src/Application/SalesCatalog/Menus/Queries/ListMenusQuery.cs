using Application.Identity.Tokens.Claims;
using System;

namespace Application.SalesCatalog.Menus.Queries;

public sealed class ListMenusQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public string? Search { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}

