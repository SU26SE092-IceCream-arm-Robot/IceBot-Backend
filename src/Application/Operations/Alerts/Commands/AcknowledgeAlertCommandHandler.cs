using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Operations.Abstractions;
using Application.Operations.Alerts.Mapping;
using Application.Operations.Alerts.Results;
using Application.Operations.Alerts.Rules;
using Application.Shared.Wrappers;
using Domain.Common;
using Domain.Operations.Enums;

namespace Application.Operations.Alerts.Commands;

public sealed class AcknowledgeAlertCommandHandler
{
    private readonly IAlertStore _store;
    private readonly IRealtimeNotificationPublisher _publisher;

    public AcknowledgeAlertCommandHandler(IAlertStore store, IRealtimeNotificationPublisher publisher)
    {
        _store = store;
        _publisher = publisher;
    }

    public async Task<ApiResult<AlertResult>> HandleAsync(
        AcknowledgeAlertCommand command,
        CancellationToken cancellationToken = default)
    {
        var outcome = await _store.ExecuteSerializedAsync(
            command.AlertId,
            ct => HandleLockedAsync(command, ct),
            cancellationToken);

        if (outcome.Notification is not null)
        {
            await _publisher.PublishAlertChangedAsync(outcome.Notification, cancellationToken);
        }

        return outcome.Result;
    }

    private async Task<LifecycleOutcome> HandleLockedAsync(
        AcknowledgeAlertCommand command,
        CancellationToken cancellationToken)
    {
        var alert = await _store.GetByIdAsync(command.AlertId, cancellationToken);
        if (alert is null)
        {
            return new LifecycleOutcome(ApiResult<AlertResult>.Fail("Alert not found.", 404), null);
        }

        if (!AlertAccessRules.CanAccess(
                command.UserContext, alert.Kiosk.OrganizationId, alert.Kiosk.StoreId, alert.KioskId))
        {
            return new LifecycleOutcome(ApiResult<AlertResult>.Fail("Access denied.", 403), null);
        }

        var oldStatus = alert.Status;
        AlertChangedEvent? notification = null;
        try
        {
            var now = DateTimeOffset.UtcNow;
            alert.Acknowledge(command.UserContext.AccountId, now);
            if (alert.Status != oldStatus)
            {
                alert.UpdatedAt = now;
                alert.UpdatedByAccountId = command.UserContext.AccountId;
                alert.Version++;
                await _store.SaveChangesAsync(cancellationToken);
                notification = ToEvent(alert, oldStatus.ToString(), now);
            }
        }
        catch (DomainRuleException ex)
        {
            return new LifecycleOutcome(ApiResult<AlertResult>.Fail(ex.Message, 400), null);
        }

        return new LifecycleOutcome(
            ApiResult<AlertResult>.Success(
                AlertResultMapper.ToResult(alert),
                oldStatus == AlertStatus.Acknowledged ? "Alert already acknowledged." : "Alert acknowledged."),
            notification);
    }

    private static AlertChangedEvent ToEvent(
        Domain.Operations.Entities.Alert alert,
        string oldStatus,
        DateTimeOffset updatedAt) =>
        new()
        {
            AlertId = alert.Id,
            KioskId = alert.KioskId,
            OrganizationId = alert.Kiosk.OrganizationId,
            StoreId = alert.Kiosk.StoreId,
            DeviceId = alert.DeviceId,
            AlertCode = alert.AlertCode,
            Severity = alert.Severity.ToString(),
            OldStatus = oldStatus,
            NewStatus = alert.Status.ToString(),
            UpdatedAt = updatedAt,
            Version = checked((int)alert.Version)
        };

    private sealed record LifecycleOutcome(ApiResult<AlertResult> Result, AlertChangedEvent? Notification);
}
