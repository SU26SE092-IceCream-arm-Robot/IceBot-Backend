using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Devices.Credentials.Commands;
using Domain.Common.Enums;
using Domain.Devices.ExecutionEndpoints;
using Domain.Operations.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Operations.Alerts.Automation;

public sealed class MqttCredentialOperationalAlertReconciler(
    IMqttCredentialAlertAutomationStore store,
    IRealtimeNotificationPublisher publisher,
    ILogger<MqttCredentialOperationalAlertReconciler> logger)
{
    public const string TimeoutCode = "MQTT_CREDENTIAL_OPERATION_TIMEOUT";
    public const string RevokeFailedCode = "MQTT_CREDENTIAL_REVOKE_FAILED";
    private const string SourceType = "ExecutionEndpointMqttCredential";

    public async Task<int> ReconcileAsync(
        Guid endpointId,
        MqttCredentialReconciliationOutcome? occurrence,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        var events = await store.ExecuteInTransactionAsync(async ct =>
        {
            await store.AcquireLockAsync(endpointId, ct);
            var endpoint = await store.GetEndpointAsync(endpointId, ct);
            if (endpoint?.MqttCredential is null)
                return new List<AlertChangedEvent>();

            var desiredCode = Classify(endpoint.MqttCredential);
            var activeAlerts = await store.ListActiveAlertsAsync(endpointId, ct);
            var events = new List<AlertChangedEvent>();
            var mutated = false;

            foreach (var alert in activeAlerts.Where(alert => alert.AlertCode != desiredCode))
            {
                var oldStatus = alert.Status.ToString();
                alert.Resolve(observedAt, "MQTT credential workflow recovered or moved to a different failure state.");
                events.Add(ToEvent(alert, endpoint, oldStatus, observedAt));
                mutated = true;
            }

            var current = desiredCode is null
                ? null
                : activeAlerts
                    .Where(alert => alert.AlertCode == desiredCode)
                    .OrderBy(alert => alert.RaisedAt)
                    .ThenBy(alert => alert.Id)
                    .FirstOrDefault();
            foreach (var duplicate in activeAlerts.Where(alert =>
                         alert.AlertCode == desiredCode && alert.Id != current?.Id))
            {
                var oldStatus = duplicate.Status.ToString();
                duplicate.Resolve(observedAt, "Duplicate MQTT credential alert reconciled.");
                events.Add(ToEvent(duplicate, endpoint, oldStatus, observedAt));
                mutated = true;
            }

            if (desiredCode is not null && current is null)
            {
                var details = Describe(endpoint, desiredCode);
                current = Alert.RaiseFromExecutionEndpoint(
                    endpoint.KioskId,
                    endpoint.Id,
                    desiredCode,
                    SeverityLevel.Error,
                    details.Title,
                    details.Message,
                    observedAt);
                await store.AddAlertAsync(current, ct);
                events.Add(ToEvent(current, endpoint, null, observedAt));
                mutated = true;
            }
            else if (current is not null && IsNewOccurrence(desiredCode, occurrence))
            {
                var details = Describe(endpoint, desiredCode!);
                current.RecordOccurrence(
                    endpoint.Id,
                    SeverityLevel.Error,
                    details.Title,
                    details.Message,
                    observedAt,
                    observedAt);
                events.Add(ToEvent(current, endpoint, current.Status.ToString(), observedAt));
                mutated = true;
            }

            if (mutated)
                await store.SaveChangesAsync(ct);
            return events;
        }, cancellationToken);

        foreach (var evt in events)
        {
            try
            {
                await publisher.PublishAlertChangedAsync(evt, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "MQTT credential alert {AlertId} committed, but SignalR publication failed.",
                    evt.AlertId);
            }
        }

        return events.Count;
    }

    private static string? Classify(ExecutionEndpointMqttCredential credential)
    {
        if (credential.Status == ExecutionEndpointMqttCredentialStatus.RevokeFailed)
            return RevokeFailedCode;
        if (credential.Status == ExecutionEndpointMqttCredentialStatus.Failed &&
            credential.LastError?.Contains("operation lease expired", StringComparison.OrdinalIgnoreCase) == true)
            return TimeoutCode;
        return null;
    }

    private static bool IsNewOccurrence(
        string? desiredCode,
        MqttCredentialReconciliationOutcome? occurrence) =>
        (desiredCode == TimeoutCode && occurrence is
            MqttCredentialReconciliationOutcome.ProvisioningMarkedFailed or
            MqttCredentialReconciliationOutcome.RotationMarkedFailed) ||
        (desiredCode == RevokeFailedCode &&
         occurrence == MqttCredentialReconciliationOutcome.RevokeRetryFailed);

    private static (string Title, string Message) Describe(
        KioskExecutionEndpoint endpoint,
        string alertCode) => alertCode == TimeoutCode
        ? ($"MQTT credential operation timed out: {endpoint.EndpointCode}",
            "Provisioning or rotation did not complete. Retry the credential operation to replace the unknown broker secret.")
        : ($"MQTT credential revocation failed: {endpoint.EndpointCode}",
            "Automatic broker revocation retry failed. Check broker connectivity and dynamic-security configuration.");

    private static AlertChangedEvent ToEvent(
        Alert alert,
        KioskExecutionEndpoint endpoint,
        string? oldStatus,
        DateTimeOffset observedAt) => new()
    {
        AlertId = alert.Id,
        KioskId = endpoint.KioskId,
        OrganizationId = endpoint.Kiosk.OrganizationId,
        StoreId = endpoint.Kiosk.StoreId,
        DeviceId = null,
        AlertCode = alert.AlertCode,
        Severity = alert.Severity.ToString(),
        OldStatus = oldStatus,
        NewStatus = alert.Status.ToString(),
        UpdatedAt = observedAt,
        Version = checked((int)alert.Version),
        OccurrenceCount = alert.OccurrenceCount,
        LastOccurredAt = alert.LastOccurredAt
    };
}
