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

public sealed record ConfigurationReleaseRobotBindingInput
{
    public Guid ProductionProgramBindingId { get; init; }
    public Guid RobotProgramId { get; init; }
    public int BindingOrder { get; init; }
    public string RequiredWorkcellCapabilityCode { get; init; } = string.Empty;

    public ConfigurationReleaseRobotBindingInput(Guid productionProgramBindingId, int bindingOrder) =>
        (ProductionProgramBindingId, BindingOrder) = (productionProgramBindingId, bindingOrder);

    public ConfigurationReleaseRobotBindingInput(Guid robotProgramId, int bindingOrder, string requiredWorkcellCapabilityCode) =>
        (RobotProgramId, BindingOrder, RequiredWorkcellCapabilityCode) = (robotProgramId, bindingOrder, requiredWorkcellCapabilityCode);
}
