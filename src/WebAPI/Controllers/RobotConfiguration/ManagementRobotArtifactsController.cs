using Application.RobotConfiguration.Commands;
using Application.RobotConfiguration.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WebAPI.Authorization;
using Domain.RobotConfiguration.Enums;
using Application.Shared.Wrappers;
using FilePath = System.IO.Path;

namespace WebAPI.Controllers.RobotConfiguration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementRobotArtifactsController : ControllerBase
{
    private readonly BulkUploadRobotArtifactsCommandHandler _bulkUploadRobotArtifactsHandler;
    private readonly BulkPublishRobotArtifactsCommandHandler _bulkPublishRobotArtifactsHandler;
    private readonly PublishRobotArtifactCommandHandler _publishRobotArtifactHandler;
    private readonly RetireRobotArtifactCommandHandler _retireRobotArtifactHandler;
    private readonly DiscardDraftRobotArtifactCommandHandler _discardDraftRobotArtifactHandler;
    private readonly ListRobotArtifactsQueryHandler _listRobotArtifactsHandler;
    private readonly GetRobotArtifactQueryHandler _getRobotArtifactHandler;
    private readonly CreateRobotArtifactReviewUrlQueryHandler _createReviewUrlHandler;

    public ManagementRobotArtifactsController(
        BulkUploadRobotArtifactsCommandHandler bulkUploadRobotArtifactsHandler,
        BulkPublishRobotArtifactsCommandHandler bulkPublishRobotArtifactsHandler,
        PublishRobotArtifactCommandHandler publishRobotArtifactHandler,
        RetireRobotArtifactCommandHandler retireRobotArtifactHandler,
        DiscardDraftRobotArtifactCommandHandler discardDraftRobotArtifactHandler,
        ListRobotArtifactsQueryHandler listRobotArtifactsHandler,
        GetRobotArtifactQueryHandler getRobotArtifactHandler,
        CreateRobotArtifactReviewUrlQueryHandler createReviewUrlHandler)
    {
        _bulkUploadRobotArtifactsHandler = bulkUploadRobotArtifactsHandler;
        _bulkPublishRobotArtifactsHandler = bulkPublishRobotArtifactsHandler;
        _publishRobotArtifactHandler = publishRobotArtifactHandler;
        _retireRobotArtifactHandler = retireRobotArtifactHandler;
        _discardDraftRobotArtifactHandler = discardDraftRobotArtifactHandler;
        _listRobotArtifactsHandler = listRobotArtifactsHandler;
        _getRobotArtifactHandler = getRobotArtifactHandler;
        _createReviewUrlHandler = createReviewUrlHandler;
    }

    [HttpGet("organizations/{organizationId:guid}/robot-artifacts")]
    [Authorize(Policy = "artifact.read")]
    public async Task<IActionResult> ListRobotArtifacts(
        Guid organizationId,
        [FromQuery] string? search,
        [FromQuery] RobotArtifactStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListRobotArtifactsQuery
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            Search = search,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _listRobotArtifactsHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("organizations/{organizationId:guid}/robot-artifacts/{artifactId:guid}")]
    [Authorize(Policy = "artifact.read")]
    public async Task<IActionResult> GetRobotArtifact(
        Guid organizationId,
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        var query = new GetRobotArtifactQuery(organizationId, artifactId)
        {
            UserContext = User.GetUserContext()
        };
        var result = await _getRobotArtifactHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/robot-artifacts/bulk")]
    [Authorize(Policy = "artifact.upload")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
    public async Task<IActionResult> BulkUploadRobotArtifacts(
        Guid organizationId,
        [FromForm] BulkUploadRobotArtifactsRequest request,
        CancellationToken cancellationToken)
    {
        var parsedManifest = RobotArtifactMultipartManifestParser.Parse(
            request.Files,
            request.ManifestJson,
            (BulkUploadRobotArtifactManifestItemRequest item) => item.FileName);
        if (!parsedManifest.Succeeded)
        {
            return BadRequest(ApiResult<object>.Fail(parsedManifest.Error!, 400));
        }

        var streams = new List<Stream>(parsedManifest.Items.Count);

        try
        {
            var items = parsedManifest.Items.Select(item =>
            {
                var file = parsedManifest.FilesByName[FilePath.GetFileName(item.FileName)];
                var stream = file.OpenReadStream();
                streams.Add(stream);
                return new BulkUploadRobotArtifactItem
                {
                    FileName = FilePath.GetFileName(file.FileName),
                    ContentType = file.ContentType,
                    ContentLengthBytes = file.Length,
                    Content = stream,
                    ArtifactCode = item.ArtifactCode,
                    ArtifactName = item.ArtifactName,
                    RuntimeTargetCode = item.RuntimeTargetCode,
                    MachineModelCode = item.MachineModelCode,
                    ExportedAt = item.ExportedAt,
                    Description = item.Description,
                    MetadataJson = item.MetadataJson
                };
            }).ToArray();

            var command = new BulkUploadRobotArtifactsCommand
            {
                UserContext = User.GetUserContext(),
                OrganizationId = organizationId,
                Items = items
            };
            var result = await _bulkUploadRobotArtifactsHandler.HandleAsync(command, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    [HttpPatch("organizations/{organizationId:guid}/robot-artifacts/{artifactId:guid}/publish")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> PublishRobotArtifact(
        Guid organizationId,
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        var command = new PublishRobotArtifactCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ArtifactId = artifactId
        };

        var result = await _publishRobotArtifactHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("organizations/{organizationId:guid}/robot-artifacts/publish-bulk")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> BulkPublishRobotArtifacts(
        Guid organizationId,
        [FromBody] BulkPublishRobotArtifactsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new BulkPublishRobotArtifactsCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            RobotArtifactIds = request.RobotArtifactIds
        };
        var result = await _bulkPublishRobotArtifactsHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("organizations/{organizationId:guid}/robot-artifacts/{artifactId:guid}/retire")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> RetireRobotArtifact(Guid organizationId, Guid artifactId, CancellationToken cancellationToken)
    {
        var result = await _retireRobotArtifactHandler.HandleAsync(new RetireRobotArtifactCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ArtifactId = artifactId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/robot-artifacts/{artifactId:guid}/review-url")]
    [Authorize(Policy = "artifact.read")]
    public async Task<IActionResult> CreateRobotArtifactReviewUrl(
        Guid organizationId,
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        var result = await _createReviewUrlHandler.HandleAsync(new CreateRobotArtifactReviewUrlQuery
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ArtifactId = artifactId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("organizations/{organizationId:guid}/robot-artifacts/{artifactId:guid}")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> DiscardDraftRobotArtifact(
        Guid organizationId,
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        var result = await _discardDraftRobotArtifactHandler.HandleAsync(new DiscardDraftRobotArtifactCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ArtifactId = artifactId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

}

public sealed class BulkUploadRobotArtifactsRequest
{
    [Required, MinLength(1)]
    public IFormFile[] Files { get; init; } = Array.Empty<IFormFile>();

    [Required]
    public string ManifestJson { get; init; } = string.Empty;
}

public sealed class BulkPublishRobotArtifactsRequest
{
    [Required, MinLength(1), MaxLength(100)]
    public IReadOnlyCollection<Guid> RobotArtifactIds { get; init; } = Array.Empty<Guid>();
}

public sealed class BulkUploadRobotArtifactManifestItemRequest
{
    [Required, StringLength(260)]
    public string FileName { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string ArtifactCode { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string ArtifactName { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string RuntimeTargetCode { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string MachineModelCode { get; init; } = string.Empty;

    public DateTimeOffset? ExportedAt { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public string? MetadataJson { get; init; }
}
