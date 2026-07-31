using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Routes.Contracts;

namespace Application.ProductionConfiguration.Routes.Commands;

public sealed class ReplaceConfigurationReleaseRoutesCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ReleaseId { get; init; }
    public string ExpectedRevision { get; init; } = string.Empty;
    public IReadOnlyCollection<ConfigurationReleaseRouteInput> Routes { get; init; } = Array.Empty<ConfigurationReleaseRouteInput>();
}

public sealed record ConfigurationReleaseRouteInput(
    Guid RecipeId,
    string RouteCode,
    int Priority,
    IReadOnlyCollection<ExecutionRouteCapabilityRequirementContract> RequiredCapabilities,
    IReadOnlyCollection<string> SupportedOptionCodes,
    IReadOnlyCollection<ConfigurationReleaseRobotBindingInput> RobotBindings);

public sealed record ConfigurationReleaseRobotBindingInput(
    Guid RobotProgramId,
    int BindingOrder,
    string RequiredWorkcellCapabilityCode);
