using Application.Identity.Tokens.Claims;

namespace Application.Operations.Alerts.Commands;

public sealed class AcknowledgeAlertCommand
{
    public required Guid AlertId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}
