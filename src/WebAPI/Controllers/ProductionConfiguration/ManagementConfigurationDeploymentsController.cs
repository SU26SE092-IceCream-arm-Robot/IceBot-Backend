using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Routes.Commands;
using Application.ProductionConfiguration.Releases.Queries;
using Application.ProductionConfiguration.Deployments.Queries;
using Application.ProductionConfiguration.Readiness.Queries;
using Application.ProductionConfiguration.Releases.ReadModels;
using Application.ProductionConfiguration.Deployments.ReadModels;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;
using Application.ProductionConfiguration.Deployments.Services;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.Controllers.ProductionConfiguration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementConfigurationDeploymentsController : ControllerBase
{
    private readonly ListConfigurationDeploymentsQueryHandler _listHandler;
    private readonly GetConfigurationDeploymentQueryHandler _getHandler;
    private readonly RollbackConfigurationDeploymentCommandHandler _rollbackHandler;
    private readonly DeployFullEdgeConfigurationCommandHandler _fullEdgeHandler;
    private readonly DeployLowCostArtifactSetCommandHandler _lowCostHandler;
    private readonly ConfigurationDeploymentPreviewHandler _deploymentPreview;

    public ManagementConfigurationDeploymentsController(
        ListConfigurationDeploymentsQueryHandler listHandler,
        GetConfigurationDeploymentQueryHandler getHandler,
        RollbackConfigurationDeploymentCommandHandler rollbackHandler,
        DeployFullEdgeConfigurationCommandHandler fullEdgeHandler,
        DeployLowCostArtifactSetCommandHandler lowCostHandler,
        ConfigurationDeploymentPreviewHandler deploymentPreview)
    {
        _listHandler = listHandler;
        _getHandler = getHandler;
        _rollbackHandler = rollbackHandler;
        _fullEdgeHandler = fullEdgeHandler;
        _lowCostHandler = lowCostHandler;
        _deploymentPreview = deploymentPreview;
    }

    [HttpGet("configuration-deployments")]
    [Authorize(Policy = "deployment.read")]
    public async Task<IActionResult> List(
        [FromQuery] Guid? organizationId, [FromQuery] Guid? storeId, [FromQuery] Guid? kioskId,
        [FromQuery] Guid? configurationReleaseId, [FromQuery] ConfigurationDeploymentProfile? profile,
        [FromQuery] ConfigurationDeploymentReadStatus? status, [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _listHandler.HandleAsync(new ListConfigurationDeploymentsQuery
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            ConfigurationReleaseId = configurationReleaseId,
            Profile = profile,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("kiosks/{kioskId:guid}/configuration-deployments")]
    [Authorize(Policy = "deployment.read")]
    public async Task<IActionResult> ListForKiosk(
        Guid kioskId,
        [FromQuery] Guid? configurationReleaseId, [FromQuery] ConfigurationDeploymentProfile? profile,
        [FromQuery] ConfigurationDeploymentReadStatus? status, [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _listHandler.HandleAsync(new ListConfigurationDeploymentsQuery
        {
            UserContext = User.GetUserContext(),
            KioskId = kioskId,
            ConfigurationReleaseId = configurationReleaseId,
            Profile = profile,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("kiosks/{kioskId:guid}/configuration-deployments/{deploymentId:guid}")]
    [Authorize(Policy = "deployment.read")]
    public async Task<IActionResult> Get(Guid kioskId, Guid deploymentId, CancellationToken cancellationToken)
    {
        var result = await _getHandler.HandleAsync(
            new GetConfigurationDeploymentQuery(kioskId, deploymentId) { UserContext = User.GetUserContext() }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("kiosks/{kioskId:guid}/configuration-deployments/{deploymentId:guid}/artifacts")]
    [Authorize(Policy = "deployment.read")]
    public async Task<IActionResult> GetArtifacts(
        Guid kioskId,
        Guid deploymentId,
        [FromServices] GetConfigurationDeploymentArtifactsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetConfigurationDeploymentArtifactsQuery(User.GetUserContext(), kioskId, deploymentId),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("kiosks/{kioskId:guid}/configuration-deployments/{deploymentId:guid}/rollback")]
    [Authorize(Policy = "release.rollback")]
    public async Task<IActionResult> Rollback(
        Guid kioskId, Guid deploymentId, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromBody] RollbackConfigurationDeploymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _rollbackHandler.HandleAsync(new RollbackConfigurationDeploymentCommand
        {
            UserContext = User.GetUserContext(),
            KioskId = kioskId,
            TargetDeploymentId = deploymentId,
            IdempotencyKey = idempotencyKey,
            Reason = request.Reason,
            ExpectedActiveDeploymentId = request.ExpectedActiveDeploymentId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("kiosks/{kioskId:guid}/configuration-deployments/full-edge")]
    [Authorize(Policy = "release.deploy")]
    public async Task<IActionResult> DeployFullEdge(
        Guid kioskId, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromBody] DeployFullEdgeConfigurationRequest request, CancellationToken cancellationToken)
    {
        var result = await _fullEdgeHandler.HandleAsync(new DeployFullEdgeConfigurationCommand
        {
            UserContext = User.GetUserContext(),
            KioskId = kioskId,
            ConfigurationReleaseId = request.ConfigurationReleaseId,
            KioskExecutionEndpointId = request.KioskExecutionEndpointId,
            IdempotencyKey = idempotencyKey,
            Reason = request.Reason,
            DeploymentPreviewChecksum = request.DeploymentPreviewChecksum,
            AcknowledgeRemainingRisk = request.AcknowledgeRemainingRisk
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("kiosks/{kioskId:guid}/configuration-deployments/preview")]
    [Authorize(Policy = "release.deploy")]
    public async Task<IActionResult> PreviewDeployment(
        Guid kioskId,
        [FromBody] ConfigurationDeploymentPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _deploymentPreview.HandleAsync(
            User.GetUserContext(),
            kioskId,
            request.ConfigurationReleaseId,
            request.KioskExecutionEndpointId,
            request.Selections.Select(item => new DeploymentPreviewSelection(
                item.ExecutionRouteId, item.RobotProgramId)).ToArray(),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("kiosks/{kioskId:guid}/configuration-deployments/low-cost")]
    [Authorize(Policy = "release.deploy")]
    public async Task<IActionResult> DeployLowCost(
        Guid kioskId, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromBody] DeployLowCostArtifactSetRequest request, CancellationToken cancellationToken)
    {
        var result = await _lowCostHandler.HandleAsync(new DeployLowCostArtifactSetCommand
        {
            UserContext = User.GetUserContext(),
            KioskId = kioskId,
            ConfigurationReleaseId = request.ConfigurationReleaseId,
            KioskExecutionEndpointId = request.KioskExecutionEndpointId,
            IdempotencyKey = idempotencyKey,
            Reason = request.Reason,
            DeploymentPreviewChecksum = request.DeploymentPreviewChecksum,
            AcknowledgeRemainingRisk = request.AcknowledgeRemainingRisk,
            Selections = request.Selections.Select(item => new DeployLowCostArtifactSelection(
                item.ExecutionRouteId, item.RobotProgramId)).ToArray()
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class DeployFullEdgeConfigurationRequest
{
    [Required]
    public Guid ConfigurationReleaseId { get; init; }

    [Required]
    public Guid KioskExecutionEndpointId { get; init; }

    [Required, StringLength(64, MinimumLength = 64)]
    public string DeploymentPreviewChecksum { get; init; } = string.Empty;

    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; init; } = string.Empty;

    public bool AcknowledgeRemainingRisk { get; init; }

}

public sealed class ConfigurationDeploymentPreviewRequest
{
    [Required] public Guid ConfigurationReleaseId { get; init; }
    public Guid? KioskExecutionEndpointId { get; init; }
    public IReadOnlyCollection<DeployLowCostArtifactSetSelectionRequest> Selections { get; init; } = [];
}

public sealed class DeployLowCostArtifactSetRequest
{
    [Required]
    public Guid ConfigurationReleaseId { get; init; }

    [Required]
    public Guid KioskExecutionEndpointId { get; init; }

    [Required, StringLength(64, MinimumLength = 64)]
    public string DeploymentPreviewChecksum { get; init; } = string.Empty;

    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; init; } = string.Empty;

    public bool AcknowledgeRemainingRisk { get; init; }

    [Required, MinLength(1)]
    public IReadOnlyCollection<DeployLowCostArtifactSetSelectionRequest> Selections { get; init; } = [];

}

public sealed class RollbackConfigurationDeploymentRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; init; } = string.Empty;

    [Required]
    public Guid ExpectedActiveDeploymentId { get; init; }
}

public sealed class DeployLowCostArtifactSetSelectionRequest
{
    [Required]
    public Guid ExecutionRouteId { get; init; }

    [Required]
    public Guid RobotProgramId { get; init; }

}
