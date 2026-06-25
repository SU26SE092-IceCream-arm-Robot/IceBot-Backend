using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Commands;

public sealed class DeployFullEdgeConfigurationCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public required Guid ConfigurationReleaseId { get; init; }
    public required Guid KioskExecutionEndpointId { get; init; }
    public DateTimeOffset? CommandExpiryAt { get; init; }
}
