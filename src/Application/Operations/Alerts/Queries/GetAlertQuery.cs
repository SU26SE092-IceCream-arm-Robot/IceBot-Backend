using Application.Identity.Tokens.Claims;

namespace Application.Operations.Alerts.Queries;

public sealed class GetAlertQuery
{
    public required Guid AlertId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}
