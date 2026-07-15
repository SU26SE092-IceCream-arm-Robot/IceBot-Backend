using Application.Identity.Tokens.Claims;

namespace Application.Orders.Management.Commands;

public enum PackagedOrderItemFulfillmentAction
{
    Fulfill = 1,
    Fail = 2
}

public sealed record SetPackagedOrderItemFulfillmentCommand(
    Guid OrderId,
    Guid OrderItemId,
    Guid FulfillmentEventId,
    CurrentUserContext UserContext,
    PackagedOrderItemFulfillmentAction Action,
    string? Reason = null);
