using Application.Identity.Tokens.Claims;
using Domain.Catalog.Enums;
using Domain.Orders.Enums;

namespace Application.Orders.Management.Queries;

public sealed class ListFulfillmentQueueQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid? KioskId { get; init; }
    public FulfillmentType? FulfillmentType { get; init; }
    public OrderItemStatus? ItemStatus { get; init; }
    public DateTimeOffset? PaidFrom { get; init; }
    public DateTimeOffset? PaidTo { get; init; }
    public bool IncludeTerminal { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
