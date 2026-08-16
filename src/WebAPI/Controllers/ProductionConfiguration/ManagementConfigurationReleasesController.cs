using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Routes.Commands;
using Application.ProductionConfiguration.Routes.Contracts;
using Application.ProductionConfiguration.Releases.Queries;
using Application.ProductionConfiguration.Deployments.Queries;
using Application.ProductionConfiguration.Readiness.Queries;
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
    private readonly RetireConfigurationReleaseCommandHandler _retireConfigurationReleaseHandler;
    private readonly CreateConfigurationReleaseCommandHandler _createConfigurationReleaseHandler;
    private readonly ReplaceConfigurationReleaseRoutesCommandHandler _replaceConfigurationReleaseRoutesHandler;
    private readonly ListConfigurationReleasesQueryHandler _listConfigurationReleasesHandler;
    private readonly GetConfigurationReleaseQueryHandler _getConfigurationReleaseHandler;
    private readonly GetConfigurationReleaseAuthoringOptionsQueryHandler _getAuthoringOptionsHandler;
    private readonly DiscardDraftConfigurationReleaseCommandHandler _discardDraftConfigurationReleaseHandler;

    public ManagementConfigurationReleasesController(
        PublishConfigurationReleaseCommandHandler publishConfigurationReleaseHandler,
        RetireConfigurationReleaseCommandHandler retireConfigurationReleaseHandler,
        CreateConfigurationReleaseCommandHandler createConfigurationReleaseHandler,
        ReplaceConfigurationReleaseRoutesCommandHandler replaceConfigurationReleaseRoutesHandler,
        ListConfigurationReleasesQueryHandler listConfigurationReleasesHandler,
        GetConfigurationReleaseQueryHandler getConfigurationReleaseHandler,
        GetConfigurationReleaseAuthoringOptionsQueryHandler getAuthoringOptionsHandler,
        DiscardDraftConfigurationReleaseCommandHandler discardDraftConfigurationReleaseHandler)
    {
        _publishConfigurationReleaseHandler = publishConfigurationReleaseHandler;
        _retireConfigurationReleaseHandler = retireConfigurationReleaseHandler;
        _createConfigurationReleaseHandler = createConfigurationReleaseHandler;
        _replaceConfigurationReleaseRoutesHandler = replaceConfigurationReleaseRoutesHandler;
        _listConfigurationReleasesHandler = listConfigurationReleasesHandler;
        _getConfigurationReleaseHandler = getConfigurationReleaseHandler;
        _getAuthoringOptionsHandler = getAuthoringOptionsHandler;
        _discardDraftConfigurationReleaseHandler = discardDraftConfigurationReleaseHandler;
    }

    [HttpGet("organizations/{organizationId:guid}/configuration-releases")]
    [Authorize(Policy = "release.read")]
    public async Task<IActionResult> ListConfigurationReleases(
        Guid organizationId,
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

    [HttpGet("organizations/{organizationId:guid}/configuration-releases/{releaseId:guid}")]
    [Authorize(Policy = "release.read")]
    public async Task<IActionResult> GetConfigurationRelease(Guid organizationId, Guid releaseId, CancellationToken cancellationToken)
    {
        var query = new GetConfigurationReleaseQuery(organizationId, releaseId) { UserContext = User.GetUserContext() };
        var result = await _getConfigurationReleaseHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("organizations/{organizationId:guid}/configuration-releases/authoring-options")]
    [Authorize(Policy = "release.read")]
    public async Task<IActionResult> GetConfigurationReleaseAuthoringOptions(
        Guid organizationId,
        [FromQuery] Guid? productVariantId,
        [FromQuery] string? search,
        [FromQuery] bool includeGlobalTemplates = false,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _getAuthoringOptionsHandler.HandleAsync(
            new GetConfigurationReleaseAuthoringOptionsQuery
            {
                UserContext = User.GetUserContext(),
                OrganizationId = organizationId,
                ProductVariantId = productVariantId,
                Search = search,
                IncludeGlobalTemplates = includeGlobalTemplates,
                Limit = limit
            },
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/configuration-releases")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> CreateConfigurationRelease(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var command = new CreateConfigurationReleaseCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId
        };
        var result = await _createConfigurationReleaseHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("organizations/{organizationId:guid}/configuration-releases/{releaseId:guid}/routes")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> ReplaceConfigurationReleaseRoutes(
        Guid organizationId,
        Guid releaseId,
        [FromBody] ReplaceConfigurationReleaseRoutesRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReplaceConfigurationReleaseRoutesCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ReleaseId = releaseId,
            ExpectedRevision = request.ExpectedRevision,
            Routes = request.Routes.Select(route => new ConfigurationReleaseRouteInput(
                route.RecipeId,
                route.RouteCode,
                route.Priority,
                route.RequiredCapabilities.Select(requirement => new ExecutionRouteCapabilityRequirementContract(
                    requirement.Code,
                    requirement.Required)).ToArray(),
                route.SupportedOptionCodes,
                route.RobotBindings.Select(binding => new ConfigurationReleaseRobotBindingInput(
                    binding.ProductionProgramBindingId,
                    binding.BindingOrder)).ToArray())).ToArray()
        };
        var result = await _replaceConfigurationReleaseRoutesHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("organizations/{organizationId:guid}/configuration-releases/{releaseId:guid}/publish")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> PublishConfigurationRelease(
        Guid organizationId,
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        var command = new PublishConfigurationReleaseCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ReleaseId = releaseId
        };

        var result = await _publishConfigurationReleaseHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("organizations/{organizationId:guid}/configuration-releases/{releaseId:guid}/retire")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> RetireConfigurationRelease(Guid organizationId, Guid releaseId, CancellationToken cancellationToken)
    {
        var result = await _retireConfigurationReleaseHandler.HandleAsync(new RetireConfigurationReleaseCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ReleaseId = releaseId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("organizations/{organizationId:guid}/configuration-releases/{releaseId:guid}")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> DiscardDraftConfigurationRelease(Guid organizationId, Guid releaseId, CancellationToken cancellationToken)
    {
        var result = await _discardDraftConfigurationReleaseHandler.HandleAsync(
            new DiscardDraftConfigurationReleaseCommand
            {
                UserContext = User.GetUserContext(),
                OrganizationId = organizationId,
                ReleaseId = releaseId
            }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

}

public sealed class ReplaceConfigurationReleaseRoutesRequest
{
    [Required, StringLength(64, MinimumLength = 64)]
    public string ExpectedRevision { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public IReadOnlyCollection<ConfigurationReleaseRouteRequest> Routes { get; init; } = Array.Empty<ConfigurationReleaseRouteRequest>();
}

public sealed class ConfigurationReleaseRouteRequest
{
    public Guid RecipeId { get; init; }

    [Required, StringLength(100)]
    public string RouteCode { get; init; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Priority { get; init; }

    public IReadOnlyCollection<ConfigurationReleaseCapabilityRequirementRequest> RequiredCapabilities { get; init; } = [];

    [Required]
    public IReadOnlyCollection<string> SupportedOptionCodes { get; init; } = [];

    [Required, MinLength(1)]
    public IReadOnlyCollection<ConfigurationReleaseRobotBindingRequest> RobotBindings { get; init; } = Array.Empty<ConfigurationReleaseRobotBindingRequest>();
}

public sealed class ConfigurationReleaseCapabilityRequirementRequest
{
    [Required, StringLength(100)]
    public string Code { get; init; } = string.Empty;

    public bool Required { get; init; } = true;
}

public sealed class ConfigurationReleaseRobotBindingRequest
{
    public Guid ProductionProgramBindingId { get; init; }

    [Range(1, int.MaxValue)]
    public int BindingOrder { get; init; }
}
