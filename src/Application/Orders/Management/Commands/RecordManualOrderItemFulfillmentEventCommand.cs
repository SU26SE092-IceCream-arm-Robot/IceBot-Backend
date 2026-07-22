using Application.Identity.Tokens.Claims;
using Application.Orders.Management.Requests;

namespace Application.Orders.Management.Commands;

public sealed record RecordManualOrderItemFulfillmentEventCommand(
    Guid OrderId,
    Guid OrderItemId,
    CurrentUserContext UserContext,
    RecordManualOrderItemFulfillmentEventRequest Request);
