using Application.ProductionConfiguration.Commands;
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

    public ManagementConfigurationReleasesController(
        PublishConfigurationReleaseCommandHandler publishConfigurationReleaseHandler,
        DeployFullEdgeConfigurationCommandHandler deployFullEdgeConfigurationHandler,
        DeployLowCostArtifactSetCommandHandler deployLowCostArtifactSetHandler)
    {
        _publishConfigurationReleaseHandler = publishConfigurationReleaseHandler;
        _deployFullEdgeConfigurationHandler = deployFullEdgeConfigurationHandler;
        _deployLowCostArtifactSetHandler = deployLowCostArtifactSetHandler;
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
