using Application.Identity.Tokens.Claims;

namespace Application.Inventory.Queries;

public sealed class GetInventorySummaryQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? StoreId { get; init; }
}
