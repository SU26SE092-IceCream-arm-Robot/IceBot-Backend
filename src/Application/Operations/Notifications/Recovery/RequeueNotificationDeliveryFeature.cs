using System.Text.Json;
using Application.Identity.Tokens.Claims;
using Application.Operations.Alerts.Notifications;
using Application.Operations.Notifications.Diagnostics;
using Application.Operations.OperationLogs.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Common.Enums;
using Domain.Operations.Entities;

namespace Application.Operations.Notifications.Recovery;

public sealed class RequeueNotificationDeliveryRequest
{
    public string Reason { get; init; } = string.Empty;
}

public sealed class RequeueNotificationDeliveryService(
    INotificationDeliveryReadStore reads,
    INotificationDeliveryStore deliveries,
    IOperationLogStore operationLogs)
{
    public async Task<ApiResult<NotificationDeliveryDiagnosticsResult>> RequeueAsync(
        CurrentUserContext user,
        Guid organizationId,
        Guid deliveryId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 3 or > 500)
            return ApiResult<NotificationDeliveryDiagnosticsResult>.Fail(
                "Requeue reason must be between 3 and 500 characters.", 400);

        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.NotificationsManage, user);
        var visible = await reads.GetAsync(organizationId, deliveryId, user.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds, cancellationToken);
        if (visible is null)
            return ApiResult<NotificationDeliveryDiagnosticsResult>.Fail("Notification delivery not found.", 404);

        try
        {
            var result = await deliveries.ExecuteInTransactionAsync(async ct =>
            {
                await deliveries.AcquireLockAsync(deliveryId, ct);
                var delivery = await deliveries.GetByOrganizationAsync(organizationId, deliveryId, ct)
                    ?? throw new DomainRuleException("Notification delivery not found.");
                delivery.Requeue(user.AccountId, DateTimeOffset.UtcNow);
                await operationLogs.AddAsync(new OperationLog
                {
                    AccountId = user.AccountId,
                    KioskId = delivery.KioskId,
                    CorrelationId = delivery.Id,
                    Action = "NotificationDeliveryRequeued",
                    Category = "Notification",
                    Severity = SeverityLevel.Info,
                    Message = "A permanently failed notification delivery was requeued.",
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        delivery.Id,
                        delivery.NotificationType,
                        Reason = reason
                    }),
                    OccurredAt = delivery.UpdatedAt!.Value,
                    OriginNodeId = Guid.Empty,
                    Version = 1,
                    SyncedAt = delivery.UpdatedAt
                }, ct);
                await deliveries.SaveChangesAsync(ct);
                return NotificationDeliveryDiagnosticsResult.From(delivery);
            }, cancellationToken);
            return ApiResult<NotificationDeliveryDiagnosticsResult>.Success(result, "Notification delivery requeued.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<NotificationDeliveryDiagnosticsResult>.Fail(ex.Message, 409);
        }
    }
}
