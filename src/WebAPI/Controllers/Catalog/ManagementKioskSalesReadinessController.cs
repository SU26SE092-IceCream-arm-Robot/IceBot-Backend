using Application.SalesCatalog.Admission.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Catalog;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/kiosks/{kioskId:guid}/sales-readiness")]
[Authorize(Policy = "kiosks.view")]
public sealed class ManagementKioskSalesReadinessController(GetKioskSalesReadinessQueryHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid kioskId, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetKioskSalesReadinessQuery(kioskId, User.GetUserContext()),
            cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
