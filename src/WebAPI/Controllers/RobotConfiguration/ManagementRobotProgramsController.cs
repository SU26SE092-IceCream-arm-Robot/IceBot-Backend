using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using System.ComponentModel.DataAnnotations;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Artifacts.Queries;
using Asp.Versioning;
using Domain.RobotConfiguration.Artifacts;
using Domain.Tenants.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.RobotConfiguration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementRobotProgramsController : ControllerBase
{
    private readonly ListRobotProgramsQueryHandler _listHandler;
    private readonly GetRobotProgramQueryHandler _getHandler;
    private readonly CreateRobotProgramCommandHandler _createHandler;
    private readonly UpdateRobotProgramCommandHandler _updateHandler;
    private readonly ReplaceRobotProgramArtifactsCommandHandler _replaceArtifactsHandler;
    private readonly ImportRawLuaRobotProgramArtifactsCommandHandler _importRawLuaArtifactsHandler;
    private readonly PublishRobotProgramCommandHandler _publishHandler;
    private readonly RetireRobotProgramCommandHandler _retireHandler;
    private readonly DiscardDraftRobotProgramCommandHandler _discardHandler;

    public ManagementRobotProgramsController(
        ListRobotProgramsQueryHandler listHandler,
        GetRobotProgramQueryHandler getHandler,
        CreateRobotProgramCommandHandler createHandler,
        UpdateRobotProgramCommandHandler updateHandler,
        ReplaceRobotProgramArtifactsCommandHandler replaceArtifactsHandler,
        ImportRawLuaRobotProgramArtifactsCommandHandler importRawLuaArtifactsHandler,
        PublishRobotProgramCommandHandler publishHandler,
        RetireRobotProgramCommandHandler retireHandler,
        DiscardDraftRobotProgramCommandHandler discardHandler)
    {
        _listHandler = listHandler;
        _getHandler = getHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _replaceArtifactsHandler = replaceArtifactsHandler;
        _importRawLuaArtifactsHandler = importRawLuaArtifactsHandler;
        _publishHandler = publishHandler;
        _retireHandler = retireHandler;
        _discardHandler = discardHandler;
    }

    [HttpGet("organizations/{organizationId:guid}/robot-programs")]
    [Authorize(Policy = "program.read")]
    public async Task<IActionResult> ListRobotPrograms(
        Guid organizationId,
        [FromQuery] string? search,
        [FromQuery] RobotProgramStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListRobotProgramsQuery
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            Search = search,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _listHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("organizations/{organizationId:guid}/robot-programs/{programId:guid}")]
    [Authorize(Policy = "program.read")]
    public async Task<IActionResult> GetRobotProgram(Guid organizationId, Guid programId, CancellationToken cancellationToken)
    {
        var query = new GetRobotProgramQuery(organizationId, programId) { UserContext = User.GetUserContext() };
        var result = await _getHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/robot-programs")]
    [Authorize(Policy = "program.manage")]
    public async Task<IActionResult> CreateRobotProgram(
        Guid organizationId,
        [FromBody] CreateRobotProgramRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRobotProgramCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            StoreId = request.StoreId,
            KioskId = request.KioskId,
            DeviceId = request.DeviceId,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description
        };
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("organizations/{organizationId:guid}/robot-programs/{programId:guid}")]
    [Authorize(Policy = "program.manage")]
    public async Task<IActionResult> UpdateRobotProgram(
        Guid organizationId,
        Guid programId,
        [FromBody] UpdateRobotProgramRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRobotProgramCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ProgramId = programId,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description
        };
        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("organizations/{organizationId:guid}/robot-programs/{programId:guid}/artifacts")]
    [Authorize(Policy = "program.manage")]
    public async Task<IActionResult> ReplaceRobotProgramArtifacts(
        Guid organizationId,
        Guid programId,
        [FromBody] ReplaceRobotProgramArtifactsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReplaceRobotProgramArtifactsCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ProgramId = programId,
            ExpectedLastModifiedAt = request.ExpectedLastModifiedAt,
            Artifacts = request.Artifacts.Select(item => new RobotProgramArtifactInput(
                item.RobotArtifactId, item.RunOrder, 1, item.ParametersJson, item.RequiredOptionCode)).ToArray()
        };
        var result = await _replaceArtifactsHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/robot-programs/{programId:guid}/raw-lua-artifacts")]
    [Authorize(Policy = "artifact.upload")]
    [Authorize(Policy = "program.manage")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = RawLuaRobotProgramImportParser.MaximumTotalExtractedBytes)]
    public async Task<IActionResult> ImportRawLuaArtifacts(
        Guid organizationId,
        Guid programId,
        [FromForm] ImportRawLuaRobotProgramArtifactsRequest request,
        CancellationToken cancellationToken)
    {
        var parsed = await RawLuaRobotProgramImportParser.ParseAsync(request.Files, request.Archive, cancellationToken);
        if (!parsed.Succeeded)
            return BadRequest(Application.Shared.Wrappers.ApiResult<object>.Fail(parsed.Error!, 400));

        try
        {
            var command = new ImportRawLuaRobotProgramArtifactsCommand
            {
                UserContext = User.GetUserContext(),
                OrganizationId = organizationId,
                ProgramId = programId,
                RuntimeTargetCode = request.RuntimeTargetCode,
                MachineModelCode = request.MachineModelCode,
                Description = request.Description,
                Artifacts = parsed.Items.Select((item, index) => new RawLuaRobotProgramArtifactInput
                {
                    FileName = item.FileName,
                    ArtifactCode = RawLuaRobotProgramImportParser.CreateArtifactCode(item.FileName, index + 1),
                    ArtifactName = RawLuaRobotProgramImportParser.CreateArtifactName(item.FileName),
                    ContentType = item.ContentType,
                    ContentLengthBytes = item.Content.Length,
                    Content = item.Content
                }).ToArray()
            };
            var result = await _importRawLuaArtifactsHandler.HandleAsync(command, cancellationToken);
            return StatusCode(result.StatusCode, result);
        }
        finally
        {
            await RawLuaRobotProgramImportParser.DisposeAsync(parsed.Items);
        }
    }

    [HttpPatch("organizations/{organizationId:guid}/robot-programs/{programId:guid}/publish")]
    [Authorize(Policy = "program.manage")]
    public async Task<IActionResult> PublishRobotProgram(Guid organizationId, Guid programId, CancellationToken cancellationToken)
    {
        var command = new PublishRobotProgramCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ProgramId = programId
        };
        var result = await _publishHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("organizations/{organizationId:guid}/robot-programs/{programId:guid}/retire")]
    [Authorize(Policy = "program.manage")]
    public async Task<IActionResult> RetireRobotProgram(Guid organizationId, Guid programId, CancellationToken cancellationToken)
    {
        var result = await _retireHandler.HandleAsync(new RetireRobotProgramCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ProgramId = programId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("organizations/{organizationId:guid}/robot-programs/{programId:guid}")]
    [Authorize(Policy = "program.manage")]
    public async Task<IActionResult> DiscardDraftRobotProgram(Guid organizationId, Guid programId, CancellationToken cancellationToken)
    {
        var result = await _discardHandler.HandleAsync(new DiscardDraftRobotProgramCommand
        {
            UserContext = User.GetUserContext(),
            OrganizationId = organizationId,
            ProgramId = programId
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class CreateRobotProgramRequest
{
    [Required, StringLength(100)]
    public string Code { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? DeviceId { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }
}

public sealed class UpdateRobotProgramRequest
{
    [Required, StringLength(100)]
    public string Code { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}

public sealed class ReplaceRobotProgramArtifactsRequest
{
    public DateTimeOffset? ExpectedLastModifiedAt { get; init; }

    [Required, MinLength(1)]
    public IReadOnlyCollection<RobotProgramArtifactRequest> Artifacts { get; init; } = Array.Empty<RobotProgramArtifactRequest>();
}

public sealed class ImportRawLuaRobotProgramArtifactsRequest
{
    public IFormFile[] Files { get; init; } = [];
    public IFormFile? Archive { get; init; }

    [Required, StringLength(100)]
    public string RuntimeTargetCode { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string MachineModelCode { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}

public sealed class RobotProgramArtifactRequest
{
    public Guid RobotArtifactId { get; init; }

    [Range(1, int.MaxValue)]
    public int RunOrder { get; init; }

    public string? ParametersJson { get; init; }

    [StringLength(100)]
    public string? RequiredOptionCode { get; init; }
}
