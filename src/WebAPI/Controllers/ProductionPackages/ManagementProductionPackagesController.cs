using System.ComponentModel.DataAnnotations;
using Application.ProductionPackages;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.ProductionPackages;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public sealed class ManagementProductionPackagesController(ProductionPackageHandlers handlers) : ControllerBase
{
    [HttpGet("production-packages")]
    [Authorize(Policy = "package.manage")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await handlers.ListManageAsync(User.GetUserContext(), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("production-packages/{packageId:guid}")]
    [Authorize(Policy = "package.manage")]
    public async Task<IActionResult> Get(Guid packageId, CancellationToken cancellationToken)
    {
        var result = await handlers.GetManageAsync(User.GetUserContext(), packageId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("production-packages")]
    [Authorize(Policy = "package.manage")]
    public async Task<IActionResult> Create([FromBody] CreateProductionPackageRequest request, CancellationToken cancellationToken)
    {
        var result = await handlers.CreatePackageAsync(User.GetUserContext(), request.Code, request.Name,
            request.Description, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("production-packages/{packageId:guid}/versions")]
    [Authorize(Policy = "package.manage")]
    public async Task<IActionResult> CreateVersion(Guid packageId, CancellationToken cancellationToken)
    {
        var result = await handlers.CreateVersionAsync(User.GetUserContext(), packageId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("production-packages/{packageId:guid}")]
    [Authorize(Policy = "package.manage")]
    public async Task<IActionResult> Update(Guid packageId, [FromBody] UpdateProductionPackageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handlers.UpdatePackageAsync(User.GetUserContext(), packageId, request.Name,
            request.Description, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("production-packages/{packageId:guid}/retire")]
    [Authorize(Policy = "package.manage")]
    public async Task<IActionResult> RetirePackage(Guid packageId, CancellationToken cancellationToken)
    {
        var result = await handlers.RetirePackageAsync(User.GetUserContext(), packageId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("production-packages/{packageId:guid}/versions/{versionId:guid}/definition")]
    [Authorize(Policy = "package.manage")]
    public async Task<IActionResult> GetDefinition(Guid packageId, Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await handlers.GetDefinitionAsync(User.GetUserContext(), packageId, versionId,
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("production-packages/{packageId:guid}/versions/{versionId:guid}/definition")]
    [Authorize(Policy = "package.manage")]
    public async Task<IActionResult> ReplaceDefinition(Guid packageId, Guid versionId,
        [FromBody] ProductionPackageDefinitionApiRequest request, CancellationToken cancellationToken)
    {
        var command = new ReplaceProductionPackageDefinitionRequest
        {
            Products = request.Products.Select(x => new PackageProductSourceRequest(x.SourceKey, x.ProductId)).ToArray(),
            Artifacts = request.Artifacts.Select(x => new PackageArtifactSourceRequest(x.SourceKey, x.RobotArtifactTemplateId)).ToArray(),
            Programs = request.Programs.Select(x => new PackageProgramBlueprintRequest(x.BlueprintCode,
                x.RuntimeTargetCode, x.MachineModelCode, x.Slots.Select(slot => new PackageProgramSlotRequest(
                    slot.SlotCode, slot.ArtifactSourceKey, slot.RequiredEffectCode, slot.Phase,
                    slot.IsRequired, slot.AllowMultiple, slot.SortHint)).ToArray())).ToArray(),
            Routes = request.Routes.Select(x => new PackageRouteBlueprintRequest(x.RouteCode, x.ProductSourceKey,
                x.ProductVariantSourceKey, x.RecipeSourceKey, x.SupportedOptionCodes,
                x.ProgramBlueprintCode, x.RequiredCapabilitiesJson, x.Priority)).ToArray()
        };
        var result = await handlers.ReplaceDefinitionAsync(User.GetUserContext(), packageId, versionId, command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("production-packages/{packageId:guid}/versions/{versionId:guid}/publish")]
    [Authorize(Policy = "package.manage")]
    public async Task<IActionResult> Publish(Guid packageId, Guid versionId, CancellationToken cancellationToken)
    {
        var result = await handlers.PublishVersionAsync(User.GetUserContext(), packageId, versionId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("production-packages/{packageId:guid}/versions/{versionId:guid}/retire")]
    [Authorize(Policy = "package.manage")]
    public async Task<IActionResult> Retire(Guid packageId, Guid versionId, CancellationToken cancellationToken)
    {
        var result = await handlers.RetireVersionAsync(User.GetUserContext(), packageId, versionId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("organizations/{organizationId:guid}/production-packages/catalog")]
    [Authorize(Policy = "package.read")]
    public async Task<IActionResult> Catalog(Guid organizationId, CancellationToken cancellationToken)
    {
        var result = await handlers.ListCatalogAsync(User.GetUserContext(), organizationId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class CreateProductionPackageRequest
{
    [Required, StringLength(100)] public string Code { get; init; } = string.Empty;
    [Required, StringLength(200)] public string Name { get; init; } = string.Empty;
    [StringLength(1000)] public string? Description { get; init; }
}

public sealed class UpdateProductionPackageRequest
{
    [Required, StringLength(200)] public string Name { get; init; } = string.Empty;
    [StringLength(1000)] public string? Description { get; init; }
}

public sealed class ProductionPackageDefinitionApiRequest
{
    [Required, MinLength(1)] public IReadOnlyCollection<PackageProductSourceApiRequest> Products { get; init; } = [];
    [Required, MinLength(1)] public IReadOnlyCollection<PackageArtifactSourceApiRequest> Artifacts { get; init; } = [];
    [Required, MinLength(1)] public IReadOnlyCollection<PackageProgramBlueprintApiRequest> Programs { get; init; } = [];
    [Required, MinLength(1)] public IReadOnlyCollection<PackageRouteBlueprintApiRequest> Routes { get; init; } = [];
}

public sealed record PackageProductSourceApiRequest([Required, StringLength(100)] string SourceKey, Guid ProductId);
public sealed record PackageArtifactSourceApiRequest([Required, StringLength(100)] string SourceKey, Guid RobotArtifactTemplateId);
public sealed record PackageProgramSlotApiRequest([Required, StringLength(100)] string SlotCode,
    [Required, StringLength(100)] string ArtifactSourceKey, [Required, StringLength(100)] string RequiredEffectCode,
    [Required, StringLength(100)] string Phase, bool IsRequired, bool AllowMultiple, int SortHint);
public sealed record PackageProgramBlueprintApiRequest([Required, StringLength(100)] string BlueprintCode,
    [Required, StringLength(100)] string RuntimeTargetCode, [Required, StringLength(100)] string MachineModelCode,
    [Required, MinLength(1)] IReadOnlyCollection<PackageProgramSlotApiRequest> Slots);
public sealed record PackageRouteBlueprintApiRequest([Required, StringLength(100)] string RouteCode,
    [Required, StringLength(100)] string ProductSourceKey,
    [Required, StringLength(100)] string ProductVariantSourceKey,
    [Required, StringLength(100)] string RecipeSourceKey,
    [Required] IReadOnlyCollection<string> SupportedOptionCodes,
    [Required, StringLength(100)] string ProgramBlueprintCode, [Required] string RequiredCapabilitiesJson, int Priority);
