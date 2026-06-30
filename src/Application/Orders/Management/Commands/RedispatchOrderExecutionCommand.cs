using Application.Identity.Tokens.Claims;

namespace Application.Orders.Management.Commands;

public sealed class RedispatchOrderExecutionCommand
{
    public required Guid OrderId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required string Reason { get; init; }
}
