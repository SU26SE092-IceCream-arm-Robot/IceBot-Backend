using Application.Devices.Telemetry;
using Application.Devices.Telemetry.Abstractions;
using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Devices.Connectivity.Abstractions;
using Domain.Devices.Connectivity;
using Domain.Tenants.Enums;
using Microsoft.Extensions.Options;

namespace Application.Devices.Connectivity.Commands;

public sealed class ReconcileKioskConnectivityCommandHandler
{
    private readonly IEdgeTelemetryIngestionStore _store;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly EdgeTelemetryIngestionOptions _options;

    public ReconcileKioskConnectivityCommandHandler(
        IEdgeTelemetryIngestionStore store,
        IRealtimeNotificationPublisher publisher,
        IOptions<EdgeTelemetryIngestionOptions> options)
    {
        _store = store;
        _publisher = publisher;
        _options = options.Value;
    }

    public async Task<bool> HandleAsync(
        ReconcileKioskConnectivityCommand command,
        CancellationToken cancellationToken = default)
    {
        var statusChangedEvent = await _store.ExecuteConnectivityReconciliationAsync(
            command.KioskId,
            ct => ReconcileLockedAsync(command, ct),
            cancellationToken);

        if (statusChangedEvent is null)
        {
            return false;
        }

        await _publisher.PublishKioskStatusChangedAsync(statusChangedEvent, cancellationToken);
        return true;
    }

    private async Task<KioskStatusChangedEvent?> ReconcileLockedAsync(
        ReconcileKioskConnectivityCommand command,
        CancellationToken cancellationToken)
    {
        var kiosk = await _store.GetKioskAsync(command.KioskId, cancellationToken);
        if (kiosk is null || kiosk.Status != KioskStatus.Active)
        {
            return null;
        }

        var cutoff = command.ObservedAt.AddSeconds(-_options.HeartbeatTimeoutSeconds);
        var connectivity = await _store.GetConnectivityAsync(command.KioskId, cancellationToken);
        if (connectivity is null || !connectivity.LastObservedAt.HasValue)
        {
            return null;
        }

        var connectivityTimestamp = connectivity.LastObservedAt.Value;
        if (connectivityTimestamp >= cutoff)
        {
            return null;
        }

        var oldConnectivity = connectivity.Status;
        if (!connectivity.MarkUnreachable(command.ObservedAt))
        {
            return null;
        }

        await _store.SaveChangesAsync(cancellationToken);

        return new KioskStatusChangedEvent
        {
            KioskId = kiosk.Id,
            OrganizationId = kiosk.OrganizationId,
            StoreId = kiosk.StoreId,
            OldConnectivity = oldConnectivity.ToString(),
            NewConnectivity = KioskConnectivityStatus.Unreachable.ToString(),
            Reason = "HeartbeatTimeout",
            UpdatedAt = command.ObservedAt,
            Version = 1
        };
    }
}
