using Application.Identity.Tokens.Claims;
using Domain.Orders.Enums;

namespace Application.Orders.Management.Queries;

public sealed class GetOrderOverviewQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public OrderStatus? Status { get; init; }
    public Guid? KioskId { get; init; }
    public int Take { get; init; } = 20;
}
