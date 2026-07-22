using Application.ProductionConfiguration.Releases.Queries;
using Application.ProductionConfiguration.Deployments.Queries;
using Application.ProductionConfiguration.Readiness.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.ProductionConfiguration;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/kiosks/{kioskId:guid}/configuration-releases")]
[Authorize(Policy = "inventory.view")]
public sealed class ManagementConfigurationInventoryReadinessController(
    GetConfigurationInventoryReadinessQueryHandler handler) : ControllerBase
{
    [HttpGet("{releaseId:guid}/inventory-readiness")]
    public async Task<IActionResult> Get(
        Guid kioskId,
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetConfigurationInventoryReadinessQuery(kioskId, releaseId, User.GetUserContext()),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
