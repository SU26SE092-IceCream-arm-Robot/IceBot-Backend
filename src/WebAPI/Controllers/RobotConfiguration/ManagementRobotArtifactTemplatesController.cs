using System.ComponentModel.DataAnnotations;
using Application.RobotConfiguration.Commands;
using Application.RobotConfiguration.Queries;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Domain.RobotConfiguration.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.RobotConfiguration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementRobotArtifactTemplatesController : ControllerBase
{
    private readonly BulkUploadRobotArtifactTemplatesCommandHandler _bulkUpload;
    private readonly ListRobotArtifactTemplatesQueryHandler _list;
    private readonly GetRobotArtifactTemplateQueryHandler _get;
    private readonly CreateRobotArtifactTemplateReviewUrlQueryHandler _review;
    private readonly PublishRobotArtifactTemplateCommandHandler _publish;
    private readonly RetireRobotArtifactTemplateCommandHandler _retire;
    private readonly DiscardDraftRobotArtifactTemplateCommandHandler _discard;
    private readonly CloneRobotArtifactTemplateCommandHandler _clone;

    public ManagementRobotArtifactTemplatesController(
        BulkUploadRobotArtifactTemplatesCommandHandler bulkUpload,
        ListRobotArtifactTemplatesQueryHandler list,
        GetRobotArtifactTemplateQueryHandler get,
        CreateRobotArtifactTemplateReviewUrlQueryHandler review,
        PublishRobotArtifactTemplateCommandHandler publish,
        RetireRobotArtifactTemplateCommandHandler retire,
        DiscardDraftRobotArtifactTemplateCommandHandler discard,
        CloneRobotArtifactTemplateCommandHandler clone)
    {
        _bulkUpload = bulkUpload; _list = list; _get = get; _review = review;
        _publish = publish; _retire = retire; _discard = discard; _clone = clone;
    }

    [HttpGet("robot-artifact-templates")]
    [Authorize(Policy = "artifact-template.read")]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] RobotArtifactStatus? status,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _list.HandleAsync(new ListRobotArtifactTemplatesQuery
        {
            UserContext = User.GetUserContext(), Search = search, Status = status, PageNumber = pageNumber, PageSize = pageSize
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("robot-artifact-templates/{templateId:guid}")]
    [Authorize(Policy = "artifact-template.read")]
    public async Task<IActionResult> Get(Guid templateId, CancellationToken cancellationToken)
    {
        var result = await _get.HandleAsync(new GetRobotArtifactTemplateQuery(templateId) { UserContext = User.GetUserContext() }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("robot-artifact-templates/bulk")]
    [Authorize(Policy = "artifact-template.manage")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
    public async Task<IActionResult> BulkUpload([FromForm] BulkUploadRobotArtifactTemplatesRequest request, CancellationToken cancellationToken)
    {
        var parsedManifest = RobotArtifactMultipartManifestParser.Parse(
            request.Files,
            request.ManifestJson,
            (RobotArtifactTemplateManifestItemRequest item) => item.FileName);
        if (!parsedManifest.Succeeded)
        {
            return BadRequest(ApiResult<object>.Fail(parsedManifest.Error!, 400));
        }

        var streams = new List<Stream>();
        try
        {
            var items = parsedManifest.Items.Select(item =>
            {
                var file = parsedManifest.FilesByName[RobotArtifactMultipartManifestParser.NormalizeFileName(item.FileName)!];
                var stream = file.OpenReadStream(); streams.Add(stream);
                return new UploadRobotArtifactTemplateCommand
                {
                    UserContext = User.GetUserContext(), FileName = RobotArtifactMultipartManifestParser.NormalizeFileName(file.FileName)!, ContentType = file.ContentType,
                    ContentLengthBytes = file.Length, Content = stream, TemplateCode = item.TemplateCode, TemplateName = item.TemplateName,
                    RuntimeTargetCode = item.RuntimeTargetCode, MachineModelCode = item.MachineModelCode, ExportedAt = item.ExportedAt,
                    Description = item.Description, MetadataJson = item.MetadataJson
                };
            }).ToArray();
            var result = await _bulkUpload.HandleAsync(new BulkUploadRobotArtifactTemplatesCommand { UserContext = User.GetUserContext(), Items = items }, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }
        finally { foreach (var stream in streams) await stream.DisposeAsync(); }
    }

    [HttpPost("robot-artifact-templates/{templateId:guid}/review-url")]
    [Authorize(Policy = "artifact-template.read")]
    public async Task<IActionResult> Review(Guid templateId, CancellationToken cancellationToken)
    {
        var result = await _review.HandleAsync(new CreateRobotArtifactTemplateReviewUrlQuery(templateId) { UserContext = User.GetUserContext() }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("robot-artifact-templates/{templateId:guid}/publish")]
    [Authorize(Policy = "artifact-template.manage")]
    public async Task<IActionResult> Publish(Guid templateId, CancellationToken cancellationToken)
    {
        var result = await _publish.HandleAsync(new PublishRobotArtifactTemplateCommand(templateId) { UserContext = User.GetUserContext() }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("robot-artifact-templates/{templateId:guid}/retire")]
    [Authorize(Policy = "artifact-template.manage")]
    public async Task<IActionResult> Retire(Guid templateId, CancellationToken cancellationToken)
    {
        var result = await _retire.HandleAsync(new RetireRobotArtifactTemplateCommand(templateId) { UserContext = User.GetUserContext() }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("robot-artifact-templates/{templateId:guid}")]
    [Authorize(Policy = "artifact-template.manage")]
    public async Task<IActionResult> Discard(Guid templateId, CancellationToken cancellationToken)
    {
        var result = await _discard.HandleAsync(
            new DiscardDraftRobotArtifactTemplateCommand(templateId)
            {
                UserContext = User.GetUserContext()
            },
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/robot-artifacts/from-template")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> Clone(Guid organizationId, [FromBody] CloneRobotArtifactTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await _clone.HandleAsync(new CloneRobotArtifactTemplateCommand
        {
            UserContext = User.GetUserContext(), OrganizationId = organizationId, TemplateId = request.TemplateId,
            ArtifactCode = request.ArtifactCode, ArtifactName = request.ArtifactName, Description = request.Description, MetadataJson = request.MetadataJson
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class BulkUploadRobotArtifactTemplatesRequest
{
    [Required, MinLength(1)] public IFormFile[] Files { get; init; } = [];
    [Required] public string ManifestJson { get; init; } = string.Empty;
}

public sealed class RobotArtifactTemplateManifestItemRequest
{
    [Required, StringLength(260)] public string FileName { get; init; } = string.Empty;
    [Required, StringLength(100)] public string TemplateCode { get; init; } = string.Empty;
    [Required, StringLength(200)] public string TemplateName { get; init; } = string.Empty;
    [Required, StringLength(100)] public string RuntimeTargetCode { get; init; } = string.Empty;
    [Required, StringLength(100)] public string MachineModelCode { get; init; } = string.Empty;
    public DateTimeOffset? ExportedAt { get; init; }
    [StringLength(500)] public string? Description { get; init; }
    public string? MetadataJson { get; init; }
}

public sealed class CloneRobotArtifactTemplateRequest
{
    [Required] public Guid TemplateId { get; init; }
    [Required, StringLength(100)] public string ArtifactCode { get; init; } = string.Empty;
    [Required, StringLength(200)] public string ArtifactName { get; init; } = string.Empty;
    [StringLength(500)] public string? Description { get; init; }
    public string? MetadataJson { get; init; }
}
