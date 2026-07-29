using Application.Identity.Tokens.Claims;

namespace Application.Orders.Management.Commands;

public sealed record RequestOrderItemProductionRemakeCommand(
    Guid OrderId,
    Guid OrderItemId,
    Guid RemakeRequestId,
    int ProductionUnitNo,
    int ProductionUnitQuantity,
    string Reason,
    CurrentUserContext UserContext,
    Guid? ProductionIncidentId = null);
