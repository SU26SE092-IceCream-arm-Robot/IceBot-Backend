using Application.Identity.Tokens.Claims;

namespace Application.Orders.Management.Queries;

public sealed class GetOrderExecutionAttemptsQuery
{
    public required Guid OrderId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
