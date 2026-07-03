using Domain.Devices.Telemetry;
using System.Text.Json;
using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Devices.Abstractions;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Domain.Common.Enums;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Microsoft.Extensions.Options;

namespace Application.Devices.Commands;

public sealed class IngestDeviceEventCommandHandler
{
    private const int MaxPayloadCharacters = 16 * 1024;
    private readonly IEdgeTelemetryIngestionStore _store;
    private readonly IAlertIngestionStore _alertStore;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly EdgeTelemetryIngestionOptions _options;

    public IngestDeviceEventCommandHandler(
        IEdgeTelemetryIngestionStore store,
        IAlertIngestionStore alertStore,
        IRealtimeNotificationPublisher publisher,
        IOptions<EdgeTelemetryIngestionOptions> options)
    {
        _store = store;
        _alertStore = alertStore;
        _publisher = publisher;
        _options = options.Value;
    }

    public async Task<ApiResult<DeviceEventIngestResult>> HandleAsync(
        IngestDeviceEventCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(command, DateTimeOffset.UtcNow);
        if (validationError is not null)
        {
            return ApiResult<DeviceEventIngestResult>.Fail(validationError, 400);
        }

        var endpoint = await _store.GetEndpointAsync(command.EndpointId, cancellationToken);
        if (endpoint is null || endpoint.KioskId != command.KioskId ||
            endpoint.Status != KioskExecutionEndpointStatus.Active ||
            endpoint.CredentialBinding?.Status != ExecutionEndpointCredentialBindingStatus.Active)
        {
            return ApiResult<DeviceEventIngestResult>.Fail("Execution endpoint authentication failed.", 401);
        }

        var boundNodeId = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.FullEdgeRuntimeId
            : endpoint.ControllerId;
        if (!boundNodeId.HasValue || boundNodeId.Value != command.OriginNodeId)
        {
            return ApiResult<DeviceEventIngestResult>.Fail(
                "Device event origin node does not match the execution endpoint identity.", 400);
        }

        var alertCorrelationKey = Alert.NormalizeCorrelationKey(command.EventType);
        var outcome = await _store.ExecuteDeviceEventIngestionAsync(
            command.EventId,
            command.KioskId,
            command.DeviceId,
            alertCorrelationKey,
            ct => IngestLockedAsync(command, ct),
            cancellationToken);

        if (outcome.Notification is not null)
        {
            await _publisher.PublishDeviceEventCreatedAsync(outcome.Notification, cancellationToken);
        }

        if (outcome.AlertNotification is not null)
        {
            await _publisher.PublishAlertChangedAsync(outcome.AlertNotification, cancellationToken);
        }

        return outcome.Result;
    }

    private async Task<IngestOutcome> IngestLockedAsync(
        IngestDeviceEventCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await _store.GetDeviceEventAsync(command.EventId, cancellationToken);
        if (existing is not null)
        {
            return new IngestOutcome(
                ApiResult<DeviceEventIngestResult>.Success(ToResult(existing, duplicate: true), "Device event already ingested."),
                null,
                null);
        }

        var device = await _store.GetDeviceAsync(command.DeviceId, cancellationToken);
        if (device?.KioskId != command.KioskId || device.Kiosk is null)
        {
            return new IngestOutcome(
                ApiResult<DeviceEventIngestResult>.Fail("Device does not belong to the reporting kiosk.", 400),
                null,
                null);
        }

        var receivedAt = DateTimeOffset.UtcNow;
        var deviceEvent = new DeviceEvent
        {
            DeviceId = device.Id,
            KioskId = command.KioskId,
            EventId = command.EventId,
            OriginNodeId = command.OriginNodeId,
            Version = 1,
            SyncedAt = receivedAt,
            CorrelationId = command.CorrelationId,
            CausationId = command.CausationId,
            EventType = command.EventType.Trim(),
            Severity = command.Severity,
            Message = command.Message.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(command.PayloadJson) ? null : command.PayloadJson,
            OccurredAt = command.OccurredAt
        };

        await _store.AddDeviceEventAsync(deviceEvent, cancellationToken);

        AlertChangedEvent? alertNotification = null;
        if (command.Severity is SeverityLevel.Error or SeverityLevel.Critical)
        {
            var correlationWindow = TimeSpan.FromMinutes(_options.AlertCorrelationWindowMinutes);
            var correlationKey = Alert.NormalizeCorrelationKey(deviceEvent.EventType);
            var alert = await _alertStore.FindCorrelatableAlertAsync(
                command.KioskId,
                device.Id,
                correlationKey,
                deviceEvent.OccurredAt - correlationWindow,
                deviceEvent.OccurredAt + correlationWindow,
                cancellationToken);
            var oldStatus = alert?.Status.ToString();

            if (alert is null)
            {
                alert = Alert.RaiseFromDeviceEvent(
                    command.KioskId,
                    device.Id,
                    deviceEvent.Id,
                    deviceEvent.EventType,
                    command.Severity,
                    Truncate($"{device.Name}: {deviceEvent.EventType}", 500)!,
                    Truncate(deviceEvent.Message, 500),
                    deviceEvent.OccurredAt,
                    command.OriginNodeId,
                    receivedAt);
                await _alertStore.AddAlertAsync(alert, cancellationToken);
            }
            else
            {
                alert.RecordOccurrence(
                    deviceEvent.Id,
                    command.Severity,
                    Truncate($"{device.Name}: {deviceEvent.EventType}", 500)!,
                    Truncate(deviceEvent.Message, 500),
                    deviceEvent.OccurredAt,
                    receivedAt);
            }

            alertNotification = new AlertChangedEvent
            {
                AlertId = alert.Id,
                KioskId = command.KioskId,
                OrganizationId = device.Kiosk.OrganizationId,
                StoreId = device.Kiosk.StoreId,
                DeviceId = device.Id,
                AlertCode = alert.AlertCode,
                Severity = alert.Severity.ToString(),
                OldStatus = oldStatus,
                NewStatus = alert.Status.ToString(),
                UpdatedAt = receivedAt,
                Version = checked((int)alert.Version),
                OccurrenceCount = alert.OccurrenceCount,
                LastOccurredAt = alert.LastOccurredAt
            };
        }

        await _store.SaveChangesAsync(cancellationToken);

        var notification = new DeviceEventCreatedEvent
        {
            DeviceEventId = deviceEvent.Id,
            KioskId = command.KioskId,
            OrganizationId = device.Kiosk.OrganizationId,
            StoreId = device.Kiosk.StoreId,
            Severity = command.Severity.ToString(),
            EventType = deviceEvent.EventType,
            Message = deviceEvent.Message,
            OccurredAt = deviceEvent.OccurredAt
        };
        return new IngestOutcome(
            ApiResult<DeviceEventIngestResult>.Success(ToResult(deviceEvent, duplicate: false), "Device event ingested.", 201),
            notification,
            alertNotification);
    }

    private string? Validate(IngestDeviceEventCommand command, DateTimeOffset receivedAt)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty || command.OriginNodeId == Guid.Empty ||
            command.DeviceId == Guid.Empty || command.EventId == Guid.Empty || command.OccurredAt == default ||
            string.IsNullOrWhiteSpace(command.EventType) || string.IsNullOrWhiteSpace(command.Message))
        {
            return "Kiosk, endpoint, origin node, device, event id, type, message, and occurred timestamp are required.";
        }

        if (command.EventType.Trim().Length > 100 || command.Message.Trim().Length > 1000)
        {
            return "Device event type or message exceeds the allowed length.";
        }

        if (command.Severity is < SeverityLevel.Warning or > SeverityLevel.Critical)
        {
            return "Device event ingest accepts Warning, Error, or Critical evidence only.";
        }

        if (command.OccurredAt > receivedAt.AddSeconds(_options.MaxFutureClockSkewSeconds))
        {
            return "Device event timestamp cannot exceed the allowed future clock skew.";
        }

        if (!string.IsNullOrWhiteSpace(command.PayloadJson))
        {
            if (command.PayloadJson.Length > MaxPayloadCharacters)
            {
                return "Device event payload exceeds 16384 characters.";
            }

            try
            {
                using var _ = JsonDocument.Parse(command.PayloadJson);
            }
            catch (JsonException)
            {
                return "Device event payload must be valid JSON.";
            }
        }

        return null;
    }

    private static DeviceEventIngestResult ToResult(DeviceEvent deviceEvent, bool duplicate) => new()
    {
        DeviceEventId = deviceEvent.Id,
        EventId = deviceEvent.EventId,
        ReceivedAt = deviceEvent.SyncedAt ?? deviceEvent.CreatedAt,
        Duplicate = duplicate
    };

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];

    private sealed record IngestOutcome(
        ApiResult<DeviceEventIngestResult> Result,
        DeviceEventCreatedEvent? Notification,
        AlertChangedEvent? AlertNotification);
}
