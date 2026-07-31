using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Domain.ProductionConfiguration.Entities;
using Application.ProductionConfiguration.Routes.Contracts;
using Application.ProductionConfiguration.Routes.Support;
using Application.ProductionConfiguration.Releases.Support;

namespace Application.ProductionConfiguration.Releases.Results;

public sealed class ConfigurationReleaseResult
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public long ReleaseNumber { get; init; }
    public string Revision { get; init; } = string.Empty;
    public string Status { get; init; } = null!;
    public string? ReleaseChecksum { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public Guid? PublishedByAccountId { get; init; }
    public int RouteCount { get; init; }
    public IReadOnlyCollection<ExecutionRouteResult> Routes { get; init; } = Array.Empty<ExecutionRouteResult>();

    public static ConfigurationReleaseResult FromEntity(ConfigurationRelease release)
    {
        return new ConfigurationReleaseResult
        {
            Id = release.Id,
            OrganizationId = release.OrganizationId,
            ReleaseNumber = release.ReleaseNumber,
            Revision = ConfigurationReleaseRevisionToken.Create(release),
            Status = release.Status.ToString(),
            ReleaseChecksum = release.ReleaseChecksum,
            PublishedAt = release.PublishedAt,
            PublishedByAccountId = release.PublishedByAccountId,
            RouteCount = release.ExecutionRoutes.Count,
            Routes = release.ExecutionRoutes
                .OrderBy(route => route.Priority)
                .ThenBy(route => route.RouteCode)
                .Select(route => new ExecutionRouteResult
                {
                    Id = route.Id,
                    ProductVariantId = route.ProductVariantId,
                    ProductVariantCode = route.ProductVariant?.Code,
                    RecipeId = route.RecipeId,
                    RecipeCode = route.Recipe?.Code,
                    RouteCode = route.RouteCode,
                    Priority = route.Priority,
                    RequiredCapabilities = ExecutionRouteRequiredCapabilitiesContract.ParseValidated(route.RequiredCapabilitiesJson)
                        .Select(requirement => new ExecutionRouteCapabilityRequirementContract(
                            requirement.Code,
                            requirement.Required))
                        .ToArray(),
                    SupportedOptionCodes = route.GetSupportedOptionCodes(),
                    ProductionDefinitionChecksum = route.ProductionDefinitionChecksum,
                    RobotBindings = route.RobotBindings
                        .OrderBy(binding => binding.BindingOrder)
                        .Select(binding => new ExecutionRouteRobotBindingResult
                        {
                            Id = binding.Id,
                            RobotProgramId = binding.RobotProgramId,
                            RobotProgramCode = binding.RobotProgram?.Code,
                            BindingOrder = binding.BindingOrder,
                            RequiredWorkcellCapabilityCode = binding.RequiredWorkcellCapabilityCode
                        })
                        .ToArray()
                })
                .ToArray()
        };
    }
}

public sealed class ExecutionRouteResult
{
    public Guid Id { get; init; }
    public Guid ProductVariantId { get; init; }
    public string? ProductVariantCode { get; init; }
    public Guid RecipeId { get; init; }
    public string? RecipeCode { get; init; }
    public string RouteCode { get; init; } = string.Empty;
    public int Priority { get; init; }
    public IReadOnlyCollection<ExecutionRouteCapabilityRequirementContract> RequiredCapabilities { get; init; } = [];
    public IReadOnlyCollection<string> SupportedOptionCodes { get; init; } = [];
    public string? ProductionDefinitionChecksum { get; init; }
    public IReadOnlyCollection<ExecutionRouteRobotBindingResult> RobotBindings { get; init; } = Array.Empty<ExecutionRouteRobotBindingResult>();
}

public sealed class ExecutionRouteRobotBindingResult
{
    public Guid Id { get; init; }
    public Guid RobotProgramId { get; init; }
    public string? RobotProgramCode { get; init; }
    public int BindingOrder { get; init; }
    public string RequiredWorkcellCapabilityCode { get; init; } = string.Empty;
}
