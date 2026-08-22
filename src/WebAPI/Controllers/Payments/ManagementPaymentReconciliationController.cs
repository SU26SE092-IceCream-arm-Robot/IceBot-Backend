using Application.Payments.Reconciliation;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Payments;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/payment-reconciliation")]
[Authorize(Policy = "payments.reconciliation.view")]
public sealed class ManagementPaymentReconciliationController : ControllerBase
{
    private readonly GetDailyPaymentReconciliationQueryHandler _dailyHandler;
    private readonly ListPaymentReconciliationDiscrepanciesQueryHandler _discrepanciesHandler;

    public ManagementPaymentReconciliationController(
        GetDailyPaymentReconciliationQueryHandler dailyHandler,
        ListPaymentReconciliationDiscrepanciesQueryHandler discrepanciesHandler)
    {
        _dailyHandler = dailyHandler;
        _discrepanciesHandler = discrepanciesHandler;
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily([FromQuery] DateOnly date, [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId, [FromQuery] Guid? kioskId, [FromQuery] string provider = "PayOS",
        CancellationToken cancellationToken = default)
    {
        var result = await _dailyHandler.HandleAsync(new DailyPaymentReconciliationQuery
        {
            UserContext = User.GetUserContext(),
            Date = date,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            Provider = provider
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("discrepancies")]
    public async Task<IActionResult> ListDiscrepancies([FromQuery] DateOnly date, [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId, [FromQuery] Guid? kioskId, [FromQuery] string provider = "PayOS",
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _discrepanciesHandler.HandleAsync(new PaymentReconciliationDiscrepanciesQuery
        {
            UserContext = User.GetUserContext(),
            Date = date,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            Provider = provider,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
