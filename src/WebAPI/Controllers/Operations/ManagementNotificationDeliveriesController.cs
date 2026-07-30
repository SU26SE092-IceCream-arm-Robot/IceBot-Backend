using Application.Operations.Notifications.Diagnostics;
using Application.Operations.Notifications.Recovery;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;

namespace WebAPI.Controllers.Operations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management/organizations/{organizationId:guid}/notification-deliveries")]
[Authorize]
public sealed class ManagementNotificationDeliveriesController(
    NotificationDeliveryOperationsService service,
    RequeueNotificationDeliveryService recovery)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "notifications.view")]
    public async Task<IActionResult> List(Guid organizationId, [FromQuery] string? status,
        [FromQuery] string? notificationType, [FromQuery] Guid? recipientAccountId,
        [FromQuery] Guid? kioskId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListAsync(new ListNotificationDeliveriesQuery(
            User.GetUserContext(), organizationId, status, notificationType, recipientAccountId,
            kioskId, from, to, pageNumber, pageSize), cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{deliveryId:guid}")]
    [Authorize(Policy = "notifications.view")]
    public async Task<IActionResult> Get(Guid organizationId, Guid deliveryId,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(
            User.GetUserContext(), organizationId, deliveryId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{deliveryId:guid}/requeue")]
    [Authorize(Policy = "notifications.manage")]
    public async Task<IActionResult> Requeue(
        Guid organizationId,
        Guid deliveryId,
        RequeueNotificationDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await recovery.RequeueAsync(
            User.GetUserContext(), organizationId, deliveryId, request.Reason, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
