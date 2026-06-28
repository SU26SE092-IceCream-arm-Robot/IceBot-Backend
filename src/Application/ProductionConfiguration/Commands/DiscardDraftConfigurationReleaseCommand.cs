using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Commands;

public sealed class DiscardDraftConfigurationReleaseCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid ReleaseId { get; init; }
}
