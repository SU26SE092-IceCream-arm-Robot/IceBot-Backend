using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Operations.Abstractions;
using Application.Operations.Alerts.Mapping;
using Application.Operations.Alerts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Operations.Enums;

namespace Application.Operations.Alerts.Commands;

public sealed class ResolveAlertCommandHandler
{
    private readonly IAlertStore _store;
    private readonly IRealtimeNotificationPublisher _publisher;

    public ResolveAlertCommandHandler(IAlertStore store, IRealtimeNotificationPublisher publisher)
    {
        _store = store;
        _publisher = publisher;
    }

    public async Task<ApiResult<AlertResult>> HandleAsync(
        ResolveAlertCommand command,
        CancellationToken cancellationToken = default)
    {
        var notes = command.Request.ResolutionNotes?.Trim();
        if (string.IsNullOrWhiteSpace(notes))
        {
            return ApiResult<AlertResult>.Fail("Resolution notes are required.", 400);
        }

        AlertChangedEvent? notification = null;
        var result = await _store.ExecuteSerializedAsync(
            command.AlertId,
            async ct =>
            {
                var outcome = await HandleLockedAsync(command, notes, ct);
                notification = outcome.Notification;
                return outcome.Result;
            },
            cancellationToken);

        if (notification is not null)
        {
            await _publisher.PublishAlertChangedAsync(notification, cancellationToken);
        }

        return result;
    }

    private async Task<LifecycleOutcome> HandleLockedAsync(
        ResolveAlertCommand command,
        string notes,
        CancellationToken cancellationToken)
    {

        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.AlertsManage, command.UserContext);
        var alert = await _store.GetAccessibleByIdAsync(
            command.AlertId,
            command.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);
        if (alert is null)
        {
            return new LifecycleOutcome(ApiResult<AlertResult>.Fail("Alert not found.", 404), null);
        }

        var oldStatus = alert.Status;
        AlertChangedEvent? notification = null;
        try
        {
            var now = DateTimeOffset.UtcNow;
            alert.Resolve(now, notes);
            if (alert.Status != oldStatus)
            {
                alert.UpdatedAt = now;
                alert.UpdatedByAccountId = command.UserContext.AccountId;
                await _store.SaveChangesAsync(cancellationToken);
                notification = new AlertChangedEvent
                {
                    AlertId = alert.Id,
                    KioskId = alert.KioskId,
                    OrganizationId = alert.Kiosk.OrganizationId,
                    StoreId = alert.Kiosk.StoreId,
                    DeviceId = alert.DeviceId,
                    AlertCode = alert.AlertCode,
                    Severity = alert.Severity.ToString(),
                    OldStatus = oldStatus.ToString(),
                    NewStatus = alert.Status.ToString(),
                    UpdatedAt = now,
                    Version = checked((int)alert.Version),
                    OccurrenceCount = alert.OccurrenceCount,
                    LastOccurredAt = alert.LastOccurredAt
                };
            }
        }
        catch (DomainRuleException ex)
        {
            return new LifecycleOutcome(ApiResult<AlertResult>.Fail(ex.Message, 400), null);
        }

        return new LifecycleOutcome(
            ApiResult<AlertResult>.Success(
                AlertResultMapper.ToResult(alert),
                oldStatus == AlertStatus.Resolved ? "Alert already resolved." : "Alert resolved."),
            notification);
    }

    private sealed record LifecycleOutcome(ApiResult<AlertResult> Result, AlertChangedEvent? Notification);
}
