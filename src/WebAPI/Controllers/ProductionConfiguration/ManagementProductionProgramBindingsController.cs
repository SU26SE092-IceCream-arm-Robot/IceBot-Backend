using Application.ProductionConfiguration.Bindings;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.ProductionConfiguration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations/{organizationId:guid}/production-program-bindings")]
public sealed class ManagementProductionProgramBindingsController(ProductionProgramBindingHandlers handlers) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "release.read")]
    public async Task<IActionResult> List(Guid organizationId, [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var parsed = Enum.TryParse<Domain.ProductionConfiguration.Entities.ProductionProgramBindingStatus>(status, true, out var value)
            ? value : (Domain.ProductionConfiguration.Entities.ProductionProgramBindingStatus?)null;
        if (!string.IsNullOrWhiteSpace(status) && !parsed.HasValue) return BadRequest("Invalid production binding status.");
        var result = await handlers.ListAsync(User.GetUserContext(), organizationId, parsed, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> Create(Guid organizationId, [FromBody] CreateProductionProgramBindingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handlers.CreateAsync(new CreateProductionProgramBindingCommand(User.GetUserContext(), organizationId,
            request.RecipeId, request.RobotProgramId, request.SupportedOptionCodes), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{bindingId:guid}/retire")]
    [Authorize(Policy = "release.publish")]
    public async Task<IActionResult> Retire(Guid organizationId, Guid bindingId, CancellationToken cancellationToken)
    {
        var result = await handlers.RetireAsync(new RetireProductionProgramBindingCommand(User.GetUserContext(), organizationId, bindingId), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}

public sealed class CreateProductionProgramBindingRequest
{
    public Guid RecipeId { get; init; }
    public Guid RobotProgramId { get; init; }
    public IReadOnlyCollection<string> SupportedOptionCodes { get; init; } = [];
}
