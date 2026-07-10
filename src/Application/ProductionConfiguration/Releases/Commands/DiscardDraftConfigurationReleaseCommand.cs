using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Releases.Commands;

public sealed class DiscardDraftConfigurationReleaseCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ReleaseId { get; init; }
}
