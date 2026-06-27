using Application.ProductionConfiguration.Commands;
using Application.ProductionConfiguration.Queries;
using Domain.ProductionConfiguration.Enums;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WebAPI.Authorization;

namespace WebAPI.Controllers.ProductionConfiguration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementConfigurationReleasesController : ControllerBase
{
    private readonly PublishConfigurationReleaseCommandHandler _publishConfigurationReleaseHandler;
    private readonly DeployFullEdgeConfigurationCommandHandler _deployFullEdgeConfigurationHandler;
    private readonly DeployLowCostArtifactSetCommandHandler _deployLowCostArtifactSetHandler;
    private readonly CreateConfigurationReleaseCommandHandler _createConfigurationReleaseHandler;
    private readonly ReplaceConfigurationReleaseRoutesCommandHandler _replaceConfigurationReleaseRoutesHandler;
    private readonly ListConfigurationReleasesQueryHandler _listConfigurationReleasesHandler;
    private readonly GetConfigurationReleaseQueryHandler _getConfigurationReleaseHandler;

    public ManagementConfigurationReleasesController(
        PublishConfigurationReleaseCommandHandler publishConfigurationReleaseHandler,
        DeployFullEdgeConfigurationCommandHandler deployFullEdgeConfigurationHandler,
        DeployLowCostArtifactSetCommandHandler deployLowCostArtifactSetHandler,
        CreateConfigurationReleaseCommandHandler createConfigurationReleaseHandler,
        ReplaceConfigurationReleaseRoutesCommandHandler replaceConfigurationReleaseRoutesHandler,
        ListConfigurationReleasesQueryHandler listConfigurationReleasesHandler,
        GetConfigurationReleaseQueryHandler getConfigurationReleaseHandler)
    {
        _publishConfigurationReleaseHandler = publishConfigurationReleaseHandler;
        _deployFullEdgeConfigurationHandler = deployFullEdgeConfigurationHandler;
        _deployLowCostArtifactSetHandler = deployLowCostArtifactSetHandler;
        _createConfigurationReleaseHandler = createConfigurationReleaseHandler;
        _replaceConfigurationReleaseRoutesHandler = replaceConfigurationReleaseRoutesHandler;
        _listConfigurationReleasesHandler = listConfigurationReleasesHandler;
        _getConfigurationReleaseHandler = getConfigurationReleaseHandler;
    }

    [HttpGet("configuration-releases")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> ListConfigurationReleases(
        [FromQuery] Guid? organizationId,
        [FromQuery] ConfigurationReleaseStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListConfigurationReleasesQuery
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _listConfigurationReleasesHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("configuration-releases/{releaseId:guid}")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> GetConfigurationRelease(Guid releaseId, CancellationToken cancellationToken)
    {
        var query = new GetConfigurationReleaseQuery(releaseId) { UserContext = User.GetUserContext() };
        var result = await _getConfigurationReleaseHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/configuration-releases")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> CreateConfigurationRelease(
        Guid organizationId,
        [FromBody] CreateConfigurationReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateConfigurationReleaseCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ReleaseManifestSchemaVersion = request.ReleaseManifestSchemaVersion
        };
        var result = await _createConfigurationReleaseHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("configuration-releases/{releaseId:guid}/routes")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> ReplaceConfigurationReleaseRoutes(
        Guid releaseId,
        [FromBody] ReplaceConfigurationReleaseRoutesRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReplaceConfigurationReleaseRoutesCommand
        {
            UserContext = User.GetUserContext(),
            ReleaseId = releaseId,
            Routes = request.Routes.Select(route => new ConfigurationReleaseRouteInput(
                route.ProductVariantId,
                route.RecipeId,
                route.RouteCode,
                route.Priority,
                route.RequiredCapabilitiesJson,
                route.RobotBindings.Select(binding => new ConfigurationReleaseRobotBindingInput(
                    binding.RobotProgramId,
                    binding.BindingOrder,
                    binding.RequiredWorkcellCapabilityCode)).ToArray())).ToArray()
        };
        var result = await _replaceConfigurationReleaseRoutesHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("configuration-releases/{releaseId:guid}/publish")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> PublishConfigurationRelease(
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        var command = new PublishConfigurationReleaseCommand
        {
            UserContext = User.GetUserContext(),
            ReleaseId = releaseId
        };

        var result = await _publishConfigurationReleaseHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("kiosks/{kioskId:guid}/configuration-deployments")]
    [Authorize(Policy = "release.deploy")]
    public async Task<IActionResult> DeployFullEdgeConfiguration(
        Guid kioskId,
        [FromBody] DeployFullEdgeConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new DeployFullEdgeConfigurationCommand
        {
            UserContext = User.GetUserContext(),
            KioskId = kioskId,
            ConfigurationReleaseId = request.ConfigurationReleaseId,
            KioskExecutionEndpointId = request.KioskExecutionEndpointId,
            CommandExpiryAt = request.CommandExpiryAt
        };

        var result = await _deployFullEdgeConfigurationHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("kiosks/{kioskId:guid}/controller-artifact-set-deployments")]
    [Authorize(Policy = "release.deploy")]
    public async Task<IActionResult> DeployLowCostArtifactSet(
        Guid kioskId,
        [FromBody] DeployLowCostArtifactSetRequest request,
        CancellationToken cancellationToken)
    {
        var command = new DeployLowCostArtifactSetCommand
        {
            UserContext = User.GetUserContext(),
            KioskId = kioskId,
            ConfigurationReleaseId = request.ConfigurationReleaseId,
            KioskExecutionEndpointId = request.KioskExecutionEndpointId,
            MaxArtifactCount = request.MaxArtifactCount,
            MaxArtifactStorageBytes = request.MaxArtifactStorageBytes,
            Selections = request.Selections.Select(selection => new DeployLowCostArtifactSelection(
                selection.ExecutionRouteId,
                selection.RobotProgramId,
                selection.RobotArtifactId,
                selection.RunOrder)).ToArray(),
            CommandExpiryAt = request.CommandExpiryAt
        };

        var result = await _deployLowCostArtifactSetHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class CreateConfigurationReleaseRequest
{
    [Range(1, int.MaxValue)]
    public int ReleaseManifestSchemaVersion { get; init; } = 1;
}

public sealed class ReplaceConfigurationReleaseRoutesRequest
{
    [Required, MinLength(1)]
    public IReadOnlyCollection<ConfigurationReleaseRouteRequest> Routes { get; init; } = Array.Empty<ConfigurationReleaseRouteRequest>();
}

public sealed class ConfigurationReleaseRouteRequest
{
    public Guid ProductVariantId { get; init; }
    public Guid RecipeId { get; init; }

    [Required, StringLength(100)]
    public string RouteCode { get; init; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Priority { get; init; }

    public string? RequiredCapabilitiesJson { get; init; }

    [Required, MinLength(1)]
    public IReadOnlyCollection<ConfigurationReleaseRobotBindingRequest> RobotBindings { get; init; } = Array.Empty<ConfigurationReleaseRobotBindingRequest>();
}

public sealed class ConfigurationReleaseRobotBindingRequest
{
    public Guid RobotProgramId { get; init; }

    [Range(1, int.MaxValue)]
    public int BindingOrder { get; init; }

    [Required, StringLength(100)]
    public string RequiredWorkcellCapabilityCode { get; init; } = string.Empty;
}

public sealed class DeployFullEdgeConfigurationRequest
{
    [Required]
    public Guid ConfigurationReleaseId { get; init; }

    [Required]
    public Guid KioskExecutionEndpointId { get; init; }

    public DateTimeOffset? CommandExpiryAt { get; init; }
}

public sealed class DeployLowCostArtifactSetRequest
{
    [Required]
    public Guid ConfigurationReleaseId { get; init; }

    [Required]
    public Guid KioskExecutionEndpointId { get; init; }

    [Range(1, int.MaxValue)]
    public int MaxArtifactCount { get; init; }

    [Range(1, long.MaxValue)]
    public long MaxArtifactStorageBytes { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyCollection<DeployLowCostArtifactSetSelectionRequest> Selections { get; init; } = Array.Empty<DeployLowCostArtifactSetSelectionRequest>();

    public DateTimeOffset? CommandExpiryAt { get; init; }
}

public sealed class DeployLowCostArtifactSetSelectionRequest
{
    [Required]
    public Guid ExecutionRouteId { get; init; }

    [Required]
    public Guid RobotProgramId { get; init; }

    [Required]
    public Guid RobotArtifactId { get; init; }

    [Range(1, int.MaxValue)]
    public int RunOrder { get; init; }
}
