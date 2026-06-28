using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Commands;

public sealed class PublishConfigurationReleaseCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid ReleaseId { get; init; }
}
