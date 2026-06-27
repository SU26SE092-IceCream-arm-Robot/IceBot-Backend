using Application.RobotConfiguration.Commands;
using Application.RobotConfiguration.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WebAPI.Authorization;
using Domain.RobotConfiguration.Enums;
using Application.Shared.Wrappers;
using System.Text.Json;
using FilePath = System.IO.Path;

namespace WebAPI.Controllers.RobotConfiguration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementRobotArtifactsController : ControllerBase
{
    private readonly BulkUploadRobotArtifactsCommandHandler _bulkUploadRobotArtifactsHandler;
    private readonly PublishRobotArtifactCommandHandler _publishRobotArtifactHandler;
    private readonly ListRobotArtifactsQueryHandler _listRobotArtifactsHandler;
    private readonly GetRobotArtifactQueryHandler _getRobotArtifactHandler;

    public ManagementRobotArtifactsController(
        BulkUploadRobotArtifactsCommandHandler bulkUploadRobotArtifactsHandler,
        PublishRobotArtifactCommandHandler publishRobotArtifactHandler,
        ListRobotArtifactsQueryHandler listRobotArtifactsHandler,
        GetRobotArtifactQueryHandler getRobotArtifactHandler)
    {
        _bulkUploadRobotArtifactsHandler = bulkUploadRobotArtifactsHandler;
        _publishRobotArtifactHandler = publishRobotArtifactHandler;
        _listRobotArtifactsHandler = listRobotArtifactsHandler;
        _getRobotArtifactHandler = getRobotArtifactHandler;
    }

    [HttpGet("organizations/{organizationId:guid}/robot-artifacts")]
    [Authorize(Policy = "artifact.upload")]
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

    [HttpGet("robot-artifacts/{artifactId:guid}")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> GetRobotArtifact(Guid artifactId, CancellationToken cancellationToken)
    {
        var query = new GetRobotArtifactQuery(artifactId) { UserContext = User.GetUserContext() };
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
        BulkUploadRobotArtifactManifestItemRequest[]? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<BulkUploadRobotArtifactManifestItemRequest[]>(
                request.ManifestJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest(ApiResult<object>.Fail("ManifestJson must be a valid JSON array.", 400));
        }

        if (manifest is null || manifest.Length == 0)
        {
            return BadRequest(ApiResult<object>.Fail("ManifestJson must contain at least one artifact item.", 400));
        }

        var manifestValidationError = ValidateBulkManifest(request.Files, manifest);
        if (manifestValidationError is not null)
        {
            return BadRequest(ApiResult<object>.Fail(manifestValidationError, 400));
        }

        var filesByName = request.Files.ToDictionary(
            file => FilePath.GetFileName(file.FileName),
            StringComparer.OrdinalIgnoreCase);
        var streams = new List<Stream>(manifest.Length);

        try
        {
            var items = manifest.Select(item =>
            {
                var file = filesByName[item.FileName];
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

    [HttpPatch("robot-artifacts/{artifactId:guid}/publish")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> PublishRobotArtifact(
        Guid artifactId,
        CancellationToken cancellationToken)
    {
        var command = new PublishRobotArtifactCommand
        {
            UserContext = User.GetUserContext(),
            ArtifactId = artifactId
        };

        var result = await _publishRobotArtifactHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private static string? ValidateBulkManifest(
        IReadOnlyCollection<IFormFile> files,
        IReadOnlyCollection<BulkUploadRobotArtifactManifestItemRequest> manifest)
    {
        if (files.Count == 0 || files.Count != manifest.Count)
        {
            return "Files and manifest items must have the same non-zero count.";
        }

        if (files.Count > 50)
        {
            return "A maximum of 50 robot artifact files is allowed per request.";
        }

        var fileNames = files.Select(file => FilePath.GetFileName(file.FileName)).ToArray();
        if (fileNames.Any(string.IsNullOrWhiteSpace) ||
            fileNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != fileNames.Length)
        {
            return "Uploaded file names must be present and unique.";
        }

        if (manifest.Any(item => !TryValidateManifestItem(item)))
        {
            return "Every manifest item requires a .lua file name, artifact metadata, and a positive run order.";
        }

        var manifestFileNames = manifest.Select(item => FilePath.GetFileName(item.FileName)).ToArray();
        if (manifestFileNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifestFileNames.Length ||
            !fileNames.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(manifestFileNames))
        {
            return "Manifest file names must uniquely match all uploaded file names.";
        }

        return null;
    }

    private static bool TryValidateManifestItem(BulkUploadRobotArtifactManifestItemRequest item)
    {
        var context = new ValidationContext(item);
        return Validator.TryValidateObject(item, context, [], validateAllProperties: true) &&
            item.FileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase);
    }

}

public sealed class BulkUploadRobotArtifactsRequest
{
    [Required, MinLength(1)]
    public IFormFile[] Files { get; init; } = Array.Empty<IFormFile>();

    [Required]
    public string ManifestJson { get; init; } = string.Empty;
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
