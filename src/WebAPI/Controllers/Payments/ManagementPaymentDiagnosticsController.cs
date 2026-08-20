using Application.Payments.PaymentSessions.Diagnostics;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Payments;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/orders/{orderId:guid}/payment-diagnostics")]
[Authorize(Policy = "payments.diagnostics.view")]
public sealed class ManagementPaymentDiagnosticsController(
    GetOrderPaymentDiagnosticsQueryHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(orderId, User.GetUserContext(), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
