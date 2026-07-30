using Application.RobotConfiguration.AuthoringImports;
using Application.RobotConfiguration.AuthoringImports.ReleaseLinkage;
using Application.RobotConfiguration.AuthoringImports.Composition;
using Application.RobotConfiguration.AuthoringImports.Workspace;
using Application.RobotConfiguration.AuthoringImports.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WebAPI.Authorization;

namespace WebAPI.Controllers.RobotConfiguration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations/{organizationId:guid}/robot-authoring-imports")]
public sealed class ManagementRobotAuthoringImportsController(
    RobotAuthoringImportHandlers handlers,
    ListRobotAuthoringImportsQueryHandler listHandler,
    CreateRobotAuthoringReleaseDraftCommandHandler createReleaseDraftHandler,
    RobotAuthoringCompositionHandlers compositionHandlers,
    RobotAuthoringWorkspaceHandler workspaceHandler) : ControllerBase
{
    private const long MaximumMultipartRequestBytes = RobotAuthoringBundleCodec.MaximumArchiveBytes + 1024 * 1024;

    [HttpGet]
    [Authorize(Policy = "program.read")]
    public async Task<IActionResult> List(
        Guid organizationId,
        [FromQuery] string? status,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] Guid? deviceId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await listHandler.HandleAsync(new ListRobotAuthoringImportsQuery(
            User.GetUserContext(), organizationId, status, storeId, kioskId, deviceId, search, pageNumber, pageSize),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Policy = "artifact.upload")]
    [RequestSizeLimit(MaximumMultipartRequestBytes)]
    public async Task<IActionResult> Upload(Guid organizationId, [FromForm] UploadRobotAuthoringImportRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
    {
        var result = await handlers.UploadAsync(new UploadRobotAuthoringImportCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            StoreId = request.StoreId,
            KioskId = request.KioskId,
            DeviceId = request.DeviceId,
            IdempotencyKey = idempotencyKey,
            FileName = request.Bundle.FileName,
            ContentType = request.Bundle.ContentType,
            ContentLengthBytes = request.Bundle.Length,
            Content = request.Bundle.OpenReadStream()
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{importId:guid}")]
    [Authorize(Policy = "program.read")]
    public async Task<IActionResult> Get(Guid organizationId, Guid importId, CancellationToken cancellationToken)
    {
        var result = await handlers.GetAsync(new GetRobotAuthoringImportQuery(
            User.GetUserContext(), organizationId, importId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{importId:guid}/workspace")]
    [Authorize(Policy = "program.read")]
    public async Task<IActionResult> GetWorkspace(
        Guid organizationId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var result = await workspaceHandler.HandleAsync(
            User.GetUserContext(), organizationId, importId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{importId:guid}/validate")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> Validate(Guid organizationId, Guid importId, CancellationToken cancellationToken)
    {
        var result = await handlers.ValidateAsync(new ValidateRobotAuthoringImportCommand(
            User.GetUserContext(), organizationId, importId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{importId:guid}/materialize")]
    [Authorize(Policy = "artifact.upload")]
    [Authorize(Policy = "program.manage")]
    public async Task<IActionResult> Materialize(
        Guid organizationId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var result = await handlers.MaterializeAsync(new MaterializeRobotAuthoringImportCommand(
            User.GetUserContext(), organizationId, importId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{importId:guid}/discard")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> Discard(Guid organizationId, Guid importId, CancellationToken cancellationToken)
    {
        var result = await handlers.DiscardAsync(new DiscardRobotAuthoringImportCommand(
            User.GetUserContext(), organizationId, importId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{importId:guid}/publish-resources")]
    [Authorize(Policy = "artifact.upload")]
    [Authorize(Policy = "program.manage")]
    public async Task<IActionResult> PublishResources(
        Guid organizationId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var result = await handlers.PublishAsync(new PublishRobotAuthoringImportCommand(
            User.GetUserContext(), organizationId, importId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{importId:guid}/create-release-draft")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> CreateReleaseDraft(
        Guid organizationId,
        Guid importId,
        [FromBody] CreateRobotAuthoringReleaseDraftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createReleaseDraftHandler.HandleAsync(
            new CreateRobotAuthoringReleaseDraftCommand(
                User.GetUserContext(),
                organizationId,
                importId,
                request.RecipeId,
                request.RequiredWorkcellCapabilityCode,
                request.SupportedOptionCodes),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{importId:guid}/preview-composition")]
    [Authorize(Policy = "artifact.upload")]
    [Authorize(Policy = "program.manage")]
    public async Task<IActionResult> PreviewComposition(Guid organizationId, Guid importId,
        [FromBody] PreviewRobotAuthoringCompositionRequest request, CancellationToken cancellationToken)
    {
        var result = await compositionHandlers.PreviewAsync(new PreviewRobotAuthoringCompositionQuery(
            User.GetUserContext(), organizationId, importId, request.RecipeId, request.SelectedOptionCodes),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{importId:guid}/confirm-composition")]
    [Authorize(Policy = "artifact.upload")]
    [Authorize(Policy = "program.manage")]
    public async Task<IActionResult> ConfirmComposition(Guid organizationId, Guid importId,
        [FromBody] ConfirmRobotAuthoringCompositionRequest request, CancellationToken cancellationToken)
    {
        var result = await compositionHandlers.ConfirmAsync(new ConfirmRobotAuthoringCompositionCommand(
            User.GetUserContext(), organizationId, importId, request.RecipeId, request.SelectedOptionCodes,
            request.PreviewChecksum), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class UploadRobotAuthoringImportRequest
{
    public required IFormFile Bundle { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? DeviceId { get; init; }
}

public sealed class CreateRobotAuthoringReleaseDraftRequest
{
    public Guid RecipeId { get; init; }
    [StringLength(100)]
    public string? RequiredWorkcellCapabilityCode { get; init; }
    public IReadOnlyCollection<string> SupportedOptionCodes { get; init; } = [];
}

public class PreviewRobotAuthoringCompositionRequest
{
    public Guid RecipeId { get; init; }
    public IReadOnlyCollection<string> SelectedOptionCodes { get; init; } = [];
}

public sealed class ConfirmRobotAuthoringCompositionRequest : PreviewRobotAuthoringCompositionRequest
{
    [Required, StringLength(64, MinimumLength = 64)]
    public required string PreviewChecksum { get; init; }
}
