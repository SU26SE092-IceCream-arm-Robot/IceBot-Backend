using Application.Identity.PlatformTechnicians;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers.Identity;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/platform/technicians")]
[Authorize]
public sealed class ManagementPlatformTechniciansController(ListPlatformTechniciansQueryHandler list, ReplacePlatformTechnicianScopesCommandHandler replace, PlatformTechnicianAccountCommandHandler accounts) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "platform-technicians.read")]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => Ok(await list.HandleAsync(search, pageNumber, pageSize, ct));

    [HttpGet("{accountId:guid}")]
    [Authorize(Policy = "platform-technicians.read")]
    public async Task<IActionResult> Get(Guid accountId, CancellationToken ct = default)
    {
        var result = await list.GetAsync(accountId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{accountId:guid}/support-scopes")]
    [Authorize(Policy = "platform-technicians.manage")]
    public async Task<IActionResult> ReplaceScopes(Guid accountId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, [FromBody] ReplaceTechnicianScopesRequest request, CancellationToken ct = default)
    {
        var value = await replace.HandleAsync(accountId, CurrentAccountId(), idempotencyKey ?? string.Empty, request, ct);
        return StatusCode(value.StatusCode, value);
    }
    [HttpPost]
    [Authorize(Policy = "platform-technicians.manage")]
    public async Task<IActionResult> Create([FromBody] CreatePlatformTechnicianRequest request, CancellationToken ct = default) { var result = await accounts.CreateAsync(request, CurrentAccountId(), ct); return StatusCode(result.StatusCode, result); }
    [HttpPut("{accountId:guid}")]
    [Authorize(Policy = "platform-technicians.manage")]
    public async Task<IActionResult> Update(Guid accountId, [FromBody] UpdatePlatformTechnicianRequest request, CancellationToken ct = default) { var result = await accounts.UpdateAsync(accountId, request, CurrentAccountId(), ct); return StatusCode(result.StatusCode, result); }
    [HttpPost("{accountId:guid}/deactivate")]
    [Authorize(Policy = "platform-technicians.manage")]
    public async Task<IActionResult> Deactivate(Guid accountId, [FromBody] TechnicianLifecycleRequest request, CancellationToken ct = default) { var result = await accounts.LifecycleAsync(accountId, request, false, CurrentAccountId(), ct); return StatusCode(result.StatusCode, result); }
    [HttpPost("{accountId:guid}/reactivate")]
    [Authorize(Policy = "platform-technicians.manage")]
    public async Task<IActionResult> Reactivate(Guid accountId, [FromBody] TechnicianLifecycleRequest request, CancellationToken ct = default) { var result = await accounts.LifecycleAsync(accountId, request, true, CurrentAccountId(), ct); return StatusCode(result.StatusCode, result); }
    private Guid? CurrentAccountId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
