using Application.Identity.Tokens.Claims;
using Application.Operations.Alerts.Requests;

namespace Application.Operations.Alerts.Commands;

public sealed class ResolveAlertCommand
{
    public required Guid AlertId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required ResolveAlertRequest Request { get; init; }
}
