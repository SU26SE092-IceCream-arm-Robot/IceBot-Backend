using Application.Identity.Tokens.Claims;

namespace Application.Orders.Management.Queries;

public sealed class GetExecutionAttemptQuery
{
    public required Guid SourceCommandId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}
