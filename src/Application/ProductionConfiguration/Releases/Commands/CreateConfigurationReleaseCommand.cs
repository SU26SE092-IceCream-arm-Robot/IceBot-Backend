using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Releases.Commands;

public sealed class CreateConfigurationReleaseCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public int ReleaseManifestSchemaVersion { get; init; } = 1;
}
