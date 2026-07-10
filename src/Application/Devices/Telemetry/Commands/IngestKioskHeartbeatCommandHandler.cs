using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Telemetry;
using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Telemetry.Results;
using Application.Shared.Wrappers;
using Domain.Common.Enums;
using Domain.Devices.Catalog;
using Domain.Tenants.Enums;
using Microsoft.Extensions.Options;

namespace Application.Devices.Telemetry.Commands;

public sealed class IngestKioskHeartbeatCommandHandler
{
    private readonly IEdgeTelemetryIngestionStore _store;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly EdgeTelemetryIngestionOptions _options;

    public IngestKioskHeartbeatCommandHandler(
        IEdgeTelemetryIngestionStore store,
        IRealtimeNotificationPublisher publisher,
        IOptions<EdgeTelemetryIngestionOptions> options)
    {
        _store = store;
        _publisher = publisher;
        _options = options.Value;
    }

    public async Task<ApiResult<HeartbeatIngestResult>> HandleAsync(
        IngestKioskHeartbeatCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(command, DateTimeOffset.UtcNow);
        if (validationError is not null)
        {
            return ApiResult<HeartbeatIngestResult>.Fail(validationError, 400);
        }

        var endpoint = await _store.GetEndpointAsync(command.EndpointId, cancellationToken);
        if (endpoint is null ||
            endpoint.KioskId != command.KioskId ||
            endpoint.Status != KioskExecutionEndpointStatus.Active ||
            endpoint.CredentialBinding?.Status != ExecutionEndpointCredentialBindingStatus.Active)
        {
            return ApiResult<HeartbeatIngestResult>.Fail("Execution endpoint authentication failed.", 401);
        }

        var boundNodeId = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.FullEdgeRuntimeId
            : endpoint.ControllerId;
        if (!boundNodeId.HasValue || boundNodeId.Value != command.OriginNodeId)
        {
            return ApiResult<HeartbeatIngestResult>.Fail(
                "Heartbeat origin node does not match the execution endpoint identity.", 400);
        }

        var outcome = await _store.ExecuteHeartbeatIngestionAsync(
            command.KioskId,
            command.OriginNodeId,
            command.HeartbeatSequence,
            ct => IngestLockedAsync(command, ct),
            cancellationToken);

        if (outcome.StatusChangedEvent is not null)
        {
            await _publisher.PublishKioskStatusChangedAsync(outcome.StatusChangedEvent, cancellationToken);
        }

        return outcome.Result;
    }

    private async Task<HeartbeatIngestionOutcome> IngestLockedAsync(
        IngestKioskHeartbeatCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await _store.GetHeartbeatAsync(
            command.KioskId,
            command.OriginNodeId,
            command.HeartbeatSequence,
            cancellationToken);
        if (existing is not null)
        {
            return new HeartbeatIngestionOutcome(
                ApiResult<HeartbeatIngestResult>.Success(ToResult(existing, duplicate: true), "Heartbeat already ingested."),
                null);
        }

        var kiosk = await _store.GetKioskAsync(command.KioskId, cancellationToken);
        if (kiosk is null)
        {
            return new HeartbeatIngestionOutcome(ApiResult<HeartbeatIngestResult>.Fail("Kiosk not found.", 404), null);
        }

        var latestHeartbeat = await _store.GetLatestHeartbeatAsync(
            command.KioskId, command.OriginNodeId, cancellationToken);
        var advancesCurrentState = latestHeartbeat?.HeartbeatSequence is null ||
            command.HeartbeatSequence > latestHeartbeat.HeartbeatSequence.Value;

        var receivedAt = DateTimeOffset.UtcNow;
        var heartbeat = new KioskHeartbeat
        {
            KioskId = command.KioskId,
            NodeId = command.OriginNodeId,
            OriginNodeId = command.OriginNodeId,
            HeartbeatSequence = command.HeartbeatSequence,
            Version = command.HeartbeatSequence,
            ReportedAt = command.ReportedAt,
            ReceivedAt = receivedAt,
            SyncedAt = receivedAt,
            Status = command.Status,
            RobotStatus = Normalize(command.RobotStatus),
            NetworkStatus = Normalize(command.NetworkStatus),
            AppVersion = Normalize(command.AppVersion),
            FirmwareVersion = Normalize(command.FirmwareVersion),
            CpuUsagePercent = command.CpuUsagePercent,
            MemoryUsagePercent = command.MemoryUsagePercent,
            DiskUsagePercent = command.DiskUsagePercent,
            PendingSyncEventCount = command.PendingSyncEventCount
        };

        if (advancesCurrentState &&
            (command.Status is KioskHeartbeatStatus.Online or KioskHeartbeatStatus.Degraded) &&
            (!kiosk.LastOnlineAt.HasValue || receivedAt > kiosk.LastOnlineAt.Value))
        {
            kiosk.LastOnlineAt = receivedAt;
        }

        var oldStatus = kiosk.Status;
        string? transitionReason = null;
        if (advancesCurrentState && command.Status == KioskHeartbeatStatus.Offline && oldStatus == KioskStatus.Active)
        {
            kiosk.Status = KioskStatus.Offline;
            transitionReason = "HeartbeatReportedOffline";
        }
        else if (advancesCurrentState &&
                 (command.Status is KioskHeartbeatStatus.Online or KioskHeartbeatStatus.Degraded) &&
                 oldStatus == KioskStatus.Offline &&
                 kiosk.Organization.Status == EntityStatus.Active &&
                 kiosk.Store.Status == EntityStatus.Active)
        {
            kiosk.Status = KioskStatus.Active;
            transitionReason = "HeartbeatRecovered";
        }

        KioskStatusChangedEvent? statusChangedEvent = null;
        if (kiosk.Status != oldStatus)
        {
            kiosk.UpdatedAt = receivedAt;
            kiosk.UpdatedByAccountId = null;
            statusChangedEvent = new KioskStatusChangedEvent
            {
                KioskId = kiosk.Id,
                OrganizationId = kiosk.OrganizationId,
                StoreId = kiosk.StoreId,
                OldStatus = oldStatus.ToString(),
                NewStatus = kiosk.Status.ToString(),
                Connectivity = command.Status.ToString(),
                Reason = transitionReason,
                UpdatedAt = receivedAt,
                Version = 1
            };
        }

        await _store.AddHeartbeatAsync(heartbeat, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);
        return new HeartbeatIngestionOutcome(
            ApiResult<HeartbeatIngestResult>.Success(
                ToResult(heartbeat, duplicate: false, stale: !advancesCurrentState),
                advancesCurrentState ? "Heartbeat ingested." : "Stale heartbeat stored without changing current connectivity.",
                201),
            statusChangedEvent);
    }

    private string? Validate(IngestKioskHeartbeatCommand command, DateTimeOffset receivedAt)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty || command.OriginNodeId == Guid.Empty ||
            command.HeartbeatSequence <= 0 || command.ReportedAt == default)
        {
            return "Kiosk, endpoint, origin node, positive heartbeat sequence, and reported timestamp are required.";
        }

        if (!Enum.IsDefined(command.Status))
        {
            return "Heartbeat status is invalid.";
        }

        if (command.ReportedAt > receivedAt.AddSeconds(_options.MaxFutureClockSkewSeconds))
        {
            return "Heartbeat timestamp cannot exceed the allowed future clock skew.";
        }

        if (!IsPercentage(command.CpuUsagePercent) || !IsPercentage(command.MemoryUsagePercent) ||
            !IsPercentage(command.DiskUsagePercent) || command.PendingSyncEventCount < 0)
        {
            return "Heartbeat resource percentages must be between 0 and 100 and pending sync count cannot be negative.";
        }

        return null;
    }

    private static bool IsPercentage(decimal? value) => !value.HasValue || value is >= 0 and <= 100;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static HeartbeatIngestResult ToResult(KioskHeartbeat heartbeat, bool duplicate, bool stale = false) => new()
    {
        HeartbeatId = heartbeat.Id,
        HeartbeatSequence = heartbeat.HeartbeatSequence!.Value,
        ReceivedAt = heartbeat.ReceivedAt,
        Duplicate = duplicate,
        Stale = stale
    };

    private sealed record HeartbeatIngestionOutcome(
        ApiResult<HeartbeatIngestResult> Result,
        KioskStatusChangedEvent? StatusChangedEvent);
}
