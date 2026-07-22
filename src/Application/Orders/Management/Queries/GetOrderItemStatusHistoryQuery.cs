using Application.Identity.Tokens.Claims;

namespace Application.Orders.Management.Queries;

public sealed class GetOrderItemStatusHistoryQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrderItemId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
