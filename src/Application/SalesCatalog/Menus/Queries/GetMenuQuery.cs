using Application.Identity.Tokens.Claims;

namespace Application.SalesCatalog.Menus.Queries;

public sealed class GetMenuQuery
{
    public Guid MenuId { get; init; }
    public Guid OrganizationId { get; init; }
    public required CurrentUserContext UserContext { get; init; }

    public GetMenuQuery(Guid menuId)
    {
        MenuId = menuId;
    }
}
