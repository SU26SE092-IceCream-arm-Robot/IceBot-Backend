using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Commands;

public sealed class ReplaceConfigurationReleaseRoutesCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ReleaseId { get; init; }
    public IReadOnlyCollection<ConfigurationReleaseRouteInput> Routes { get; init; } = Array.Empty<ConfigurationReleaseRouteInput>();
}

public sealed record ConfigurationReleaseRouteInput(
    Guid RecipeId,
    string RouteCode,
    int Priority,
    string? RequiredCapabilitiesJson,
    IReadOnlyCollection<ConfigurationReleaseRobotBindingInput> RobotBindings);

public sealed record ConfigurationReleaseRobotBindingInput(
    Guid RobotProgramId,
    int BindingOrder,
    string RequiredWorkcellCapabilityCode);
