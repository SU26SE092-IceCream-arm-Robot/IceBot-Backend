using System.ComponentModel.DataAnnotations;
using Application.RobotConfiguration.ArtifactContracts;
using Asp.Versioning;
using Domain.RobotConfiguration.ArtifactContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.RobotConfiguration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementRobotArtifactTechnicalContractsController(
    RobotArtifactTechnicalContractHandlers handlers,
    AssignRobotArtifactTechnicalContractHandler assignment) : ControllerBase
{
    [HttpGet("robot-artifact-technical-contracts")]
    [Authorize(Policy = "artifact-template.read")]
    public async Task<IActionResult> ListGlobal(
        [FromQuery] RobotArtifactContractStatus? status,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await handlers.ListAsync(
            new ListRobotArtifactTechnicalContractsQuery(
                User.GetUserContext(), null, status, search, pageNumber, pageSize), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("robot-artifact-technical-contracts/{contractId:guid}")]
    [Authorize(Policy = "artifact-template.read")]
    public async Task<IActionResult> GetGlobal(Guid contractId, CancellationToken cancellationToken)
    {
        var result = await handlers.GetAsync(
            new GetRobotArtifactTechnicalContractQuery(User.GetUserContext(), null, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("robot-artifact-technical-contracts")]
    [Authorize(Policy = "artifact-template.manage")]
    public async Task<IActionResult> CreateGlobal(
        [FromBody] RobotArtifactTechnicalContractRequest request, CancellationToken cancellationToken)
    {
        var result = await handlers.CreateAsync(ToCommand(request, null), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("robot-artifact-technical-contracts/{contractId:guid}/publish")]
    [Authorize(Policy = "artifact-template.manage")]
    public async Task<IActionResult> PublishGlobal(Guid contractId, CancellationToken cancellationToken)
    {
        var result = await handlers.PublishAsync(
            new PublishRobotArtifactTechnicalContractCommand(User.GetUserContext(), null, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("robot-artifact-technical-contracts/{contractId:guid}")]
    [Authorize(Policy = "artifact-template.manage")]
    public async Task<IActionResult> ReplaceGlobal(Guid contractId,
        [FromBody] RobotArtifactTechnicalContractDefinitionRequest request, CancellationToken cancellationToken)
    {
        var result = await handlers.ReplaceAsync(ToReplaceCommand(request, null, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("robot-artifact-technical-contracts/{contractId:guid}/validation-preview")]
    [Authorize(Policy = "artifact-template.manage")]
    public async Task<IActionResult> ValidateGlobal(Guid contractId, CancellationToken cancellationToken)
    {
        var result = await handlers.ValidateAsync(
            new ValidateRobotArtifactTechnicalContractCommand(User.GetUserContext(), null, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("robot-artifact-technical-contracts/{contractId:guid}/retire")]
    [Authorize(Policy = "artifact-template.manage")]
    public async Task<IActionResult> RetireGlobal(Guid contractId, CancellationToken cancellationToken)
    {
        var result = await handlers.RetireAsync(
            new RetireRobotArtifactTechnicalContractCommand(User.GetUserContext(), null, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("robot-artifact-technical-contracts/{contractId:guid}")]
    [Authorize(Policy = "artifact-template.manage")]
    public async Task<IActionResult> DiscardGlobal(Guid contractId, CancellationToken cancellationToken)
    {
        var result = await handlers.DiscardAsync(
            new DiscardRobotArtifactTechnicalContractCommand(User.GetUserContext(), null, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("organizations/{organizationId:guid}/robot-artifact-technical-contracts")]
    [Authorize(Policy = "artifact.read")]
    public async Task<IActionResult> ListOrganization(
        Guid organizationId,
        [FromQuery] RobotArtifactContractStatus? status,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await handlers.ListAsync(
            new ListRobotArtifactTechnicalContractsQuery(
                User.GetUserContext(), organizationId, status, search, pageNumber, pageSize), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("organizations/{organizationId:guid}/robot-artifact-technical-contracts/{contractId:guid}")]
    [Authorize(Policy = "artifact.read")]
    public async Task<IActionResult> GetOrganization(Guid organizationId, Guid contractId,
        CancellationToken cancellationToken)
    {
        var result = await handlers.GetAsync(
            new GetRobotArtifactTechnicalContractQuery(User.GetUserContext(), organizationId, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/robot-artifact-technical-contracts")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> CreateOrganization(
        Guid organizationId, [FromBody] RobotArtifactTechnicalContractRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handlers.CreateAsync(ToCommand(request, organizationId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/robot-artifact-technical-contracts/import-sidecars")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> ImportOrganizationSidecars(Guid organizationId,
        [FromBody] BulkRobotArtifactSidecarImportRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Length is < 1 or > 50)
            return BadRequest(Application.Shared.Wrappers.ApiResult<object>.Fail(
                "Sidecar import requires 1 to 50 items.", 400));

        var results = new List<RobotArtifactSidecarImportItemResult>(request.Items.Length);
        foreach (var item in request.Items)
        {
            if (item.SchemaVersion != 1)
            {
                results.Add(new RobotArtifactSidecarImportItemResult(
                    item.ArtifactCode, null, false, 400, "Unsupported IceBot sidecar schema version."));
                continue;
            }

            var result = await handlers.ImportSidecarAsync(ToSidecarCommand(item, organizationId), cancellationToken);
            results.Add(new RobotArtifactSidecarImportItemResult(
                item.ArtifactCode,
                result.Data?.Id,
                result.Succeeded,
                result.StatusCode,
                result.Message));
        }

        return Ok(Application.Shared.Wrappers.ApiResult<IReadOnlyCollection<RobotArtifactSidecarImportItemResult>>
            .Success(results));
    }

    [HttpPatch("organizations/{organizationId:guid}/robot-artifact-technical-contracts/{contractId:guid}/publish")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> PublishOrganization(
        Guid organizationId, Guid contractId, CancellationToken cancellationToken)
    {
        var result = await handlers.PublishAsync(
            new PublishRobotArtifactTechnicalContractCommand(User.GetUserContext(), organizationId, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("organizations/{organizationId:guid}/robot-artifact-technical-contracts/{contractId:guid}")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> ReplaceOrganization(Guid organizationId, Guid contractId,
        [FromBody] RobotArtifactTechnicalContractDefinitionRequest request, CancellationToken cancellationToken)
    {
        var result = await handlers.ReplaceAsync(ToReplaceCommand(request, organizationId, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("organizations/{organizationId:guid}/robot-artifact-technical-contracts/{contractId:guid}/validation-preview")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> ValidateOrganization(Guid organizationId, Guid contractId,
        CancellationToken cancellationToken)
    {
        var result = await handlers.ValidateAsync(
            new ValidateRobotArtifactTechnicalContractCommand(User.GetUserContext(), organizationId, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("organizations/{organizationId:guid}/robot-artifact-technical-contracts/{contractId:guid}/retire")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> RetireOrganization(Guid organizationId, Guid contractId,
        CancellationToken cancellationToken)
    {
        var result = await handlers.RetireAsync(
            new RetireRobotArtifactTechnicalContractCommand(User.GetUserContext(), organizationId, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("organizations/{organizationId:guid}/robot-artifact-technical-contracts/{contractId:guid}")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> DiscardOrganization(Guid organizationId, Guid contractId,
        CancellationToken cancellationToken)
    {
        var result = await handlers.DiscardAsync(
            new DiscardRobotArtifactTechnicalContractCommand(User.GetUserContext(), organizationId, contractId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("robot-artifact-templates/{templateId:guid}/technical-contract")]
    [Authorize(Policy = "artifact-template.manage")]
    public async Task<IActionResult> AssignTemplate(Guid templateId,
        [FromBody] AssignRobotArtifactTechnicalContractRequest request, CancellationToken cancellationToken)
    {
        var result = await assignment.AssignTemplateAsync(User.GetUserContext(), templateId,
            request.TechnicalContractId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("organizations/{organizationId:guid}/robot-artifacts/{artifactId:guid}/technical-contract")]
    [Authorize(Policy = "artifact.upload")]
    public async Task<IActionResult> AssignArtifact(Guid organizationId, Guid artifactId,
        [FromBody] AssignRobotArtifactTechnicalContractRequest request, CancellationToken cancellationToken)
    {
        var result = await assignment.AssignArtifactAsync(User.GetUserContext(), organizationId, artifactId,
            request.TechnicalContractId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private CreateRobotArtifactTechnicalContractCommand ToCommand(
        RobotArtifactTechnicalContractRequest request, Guid? organizationId) => new()
    {
        UserContext = User.GetUserContext(),
        OrganizationId = organizationId,
        ContractCode = request.ContractCode,
        ContractVersion = request.ContractVersion,
        RuntimeTargetCode = request.RuntimeTargetCode,
        MachineModelCode = request.MachineModelCode,
        Effects = request.Effects.Select(x => new RobotArtifactEffectRequest(x.EffectCode, x.EffectKind,
            x.IngredientCode, x.OptionCode, x.QuantityMode, x.FixedQuantity, x.Unit,
            x.RequiredWorkcellCapabilityCode)).ToArray(),
        OrderingConstraints = request.OrderingConstraints.Select(x => new RobotArtifactOrderingConstraintRequest(
            x.ConstraintType, x.Value, x.SortHint)).ToArray()
    };

    private ReplaceRobotArtifactTechnicalContractCommand ToReplaceCommand(
        RobotArtifactTechnicalContractDefinitionRequest request, Guid? organizationId, Guid contractId) => new(
        User.GetUserContext(), organizationId, contractId,
        request.Effects.Select(ToEffect).ToArray(),
        request.OrderingConstraints.Select(ToConstraint).ToArray());

    private ImportRobotArtifactTechnicalContractSidecarCommand ToSidecarCommand(
        RobotArtifactSidecarImportRequest request, Guid organizationId) => new()
    {
        UserContext = User.GetUserContext(),
        OrganizationId = organizationId,
        ContractCode = request.ArtifactCode,
        ContractVersion = request.ContractVersion,
        RuntimeTargetCode = request.RuntimeTargetCode,
        MachineModelCode = request.MachineModelCode,
        Effects = request.Effects.Select(ToEffect).ToArray(),
        OrderingConstraints = request.OrderingConstraints.Select(ToConstraint).ToArray()
    };

    private static RobotArtifactEffectRequest ToEffect(RobotArtifactEffectContractRequest x) => new(
        x.EffectCode, x.EffectKind, x.IngredientCode, x.OptionCode, x.QuantityMode, x.FixedQuantity,
        x.Unit, x.RequiredWorkcellCapabilityCode);

    private static RobotArtifactOrderingConstraintRequest ToConstraint(RobotArtifactOrderingContractRequest x) =>
        new(x.ConstraintType, x.Value, x.SortHint);
}

public sealed class AssignRobotArtifactTechnicalContractRequest
{
    public Guid TechnicalContractId { get; init; }
}

public sealed class RobotArtifactTechnicalContractRequest
{
    [Required, StringLength(100)] public string ContractCode { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int ContractVersion { get; init; } = 1;
    [Required, StringLength(100)] public string RuntimeTargetCode { get; init; } = string.Empty;
    [Required, StringLength(100)] public string MachineModelCode { get; init; } = string.Empty;
    [Required, MinLength(1)] public IReadOnlyCollection<RobotArtifactEffectContractRequest> Effects { get; init; } = [];
    public IReadOnlyCollection<RobotArtifactOrderingContractRequest> OrderingConstraints { get; init; } = [];
}

public class RobotArtifactTechnicalContractDefinitionRequest
{
    [Required, MinLength(1)] public RobotArtifactEffectContractRequest[] Effects { get; init; } = [];
    public RobotArtifactOrderingContractRequest[] OrderingConstraints { get; init; } = [];
}

public sealed class RobotArtifactSidecarImportRequest : RobotArtifactTechnicalContractDefinitionRequest
{
    [Range(1, 1)] public int SchemaVersion { get; init; } = 1;
    [Required, StringLength(100)] public string ArtifactCode { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int ContractVersion { get; init; } = 1;
    [Required, StringLength(100)] public string RuntimeTargetCode { get; init; } = string.Empty;
    [Required, StringLength(100)] public string MachineModelCode { get; init; } = string.Empty;
}

public sealed class BulkRobotArtifactSidecarImportRequest
{
    [Required, MinLength(1), MaxLength(50)]
    public RobotArtifactSidecarImportRequest[] Items { get; init; } = [];
}

public sealed record RobotArtifactSidecarImportItemResult(
    string ArtifactCode,
    Guid? TechnicalContractId,
    bool Succeeded,
    int StatusCode,
    string? Message);

public sealed class RobotArtifactEffectContractRequest
{
    [Required, StringLength(100)] public string EffectCode { get; init; } = string.Empty;
    public RobotArtifactEffectKind EffectKind { get; init; }
    [StringLength(100)] public string? IngredientCode { get; init; }
    [StringLength(100)] public string? OptionCode { get; init; }
    public RobotArtifactQuantityMode QuantityMode { get; init; }
    public decimal? FixedQuantity { get; init; }
    [StringLength(50)] public string? Unit { get; init; }
    [StringLength(100)] public string? RequiredWorkcellCapabilityCode { get; init; }
}

public sealed class RobotArtifactOrderingContractRequest
{
    public RobotArtifactOrderingConstraintType ConstraintType { get; init; }
    [Required, StringLength(100)] public string Value { get; init; } = string.Empty;
    public int SortHint { get; init; }
}
