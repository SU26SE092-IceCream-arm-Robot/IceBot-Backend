using Application.RobotConfiguration.Commands;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WebAPI.Authorization;

namespace WebAPI.Controllers.RobotConfiguration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementRobotArtifactsController : ControllerBase
{
    private readonly UploadRobotArtifactCommandHandler _uploadRobotArtifactHandler;
    private readonly PublishRobotArtifactCommandHandler _publishRobotArtifactHandler;
    private readonly PublishRobotProgramCommandHandler _publishRobotProgramHandler;

    public ManagementRobotArtifactsController(
        UploadRobotArtifactCommandHandler uploadRobotArtifactHandler,
        PublishRobotArtifactCommandHandler publishRobotArtifactHandler,
        PublishRobotProgramCommandHandler publishRobotProgramHandler)
    {
        _uploadRobotArtifactHandler = uploadRobotArtifactHandler;
        _publishRobotArtifactHandler = publishRobotArtifactHandler;
        _publishRobotProgramHandler = publishRobotProgramHandler;
    }

    [HttpPost("organizations/{organizationId:guid}/robot-artifacts")]
    [Authorize(Policy = "artifact.upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadRobotArtifact(
        Guid organizationId,
        [FromForm] UploadRobotArtifactRequest request,
        CancellationToken cancellationToken)
    {
        await using var content = request.File.OpenReadStream();
        var command = new UploadRobotArtifactCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ArtifactCode = request.ArtifactCode,
            ArtifactName = request.ArtifactName,
            FileName = request.File.FileName,
            RuntimeTargetCode = request.RuntimeTargetCode,
            MachineModelCode = request.MachineModelCode,
            ContentType = request.File.ContentType,
            ContentLengthBytes = request.File.Length,
            Content = content,
            ExportedAt = request.ExportedAt,
            Description = request.Description,
            MetadataJson = request.MetadataJson
        };

        var result = await _uploadRobotArtifactHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
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

    [HttpPatch("robot-programs/{programId:guid}/publish")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> PublishRobotProgram(
        Guid programId,
        CancellationToken cancellationToken)
    {
        var command = new PublishRobotProgramCommand
        {
            UserContext = User.GetUserContext(),
            ProgramId = programId
        };

        var result = await _publishRobotProgramHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class UploadRobotArtifactRequest
{
    [Required]
    [StringLength(100)]
    public string ArtifactCode { get; init; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ArtifactName { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string RuntimeTargetCode { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string MachineModelCode { get; init; } = string.Empty;

    public DateTimeOffset? ExportedAt { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public string? MetadataJson { get; init; }

    [Required]
    public IFormFile File { get; init; } = null!;
}
