using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Connectivity.Results;
using Application.Devices.Telemetry;
using Application.Shared.Wrappers;
using Domain.Common;
using Domain.Devices.ExecutionEndpoints;
using Microsoft.Extensions.Options;

namespace Application.Devices.Connectivity.Commands;

public sealed class ReplaceExecutionEndpointReportedDevicesCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid SourceExecutorId { get; init; }
    public required long SnapshotRevision { get; init; }
    public required DateTimeOffset ObservedAt { get; init; }
    public required IReadOnlyCollection<ReportedDeviceSnapshotItem> Devices { get; init; }
}

public sealed class ReplaceExecutionEndpointReportedDevicesCommandHandler
{
    private readonly IExecutionEndpointReportedDeviceStore _store;
    private readonly EdgeTelemetryIngestionOptions _options;

    public ReplaceExecutionEndpointReportedDevicesCommandHandler(
        IExecutionEndpointReportedDeviceStore store,
        IOptions<EdgeTelemetryIngestionOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public async Task<ApiResult<ReportedDeviceSnapshotResult>> HandleAsync(
        ReplaceExecutionEndpointReportedDevicesCommand command,
        CancellationToken ct = default)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty ||
            command.SourceExecutorId == Guid.Empty || command.SnapshotRevision <= 0)
            return ApiResult<ReportedDeviceSnapshotResult>.Fail("Reported-device source identity and positive revision are required.", 400);
        if (command.ObservedAt == default || command.ObservedAt > DateTimeOffset.UtcNow.AddSeconds(_options.MaxFutureClockSkewSeconds))
            return ApiResult<ReportedDeviceSnapshotResult>.Fail("Reported-device observation timestamp is invalid.", 400);
        if (command.Devices is null || command.Devices.Count > 50)
            return ApiResult<ReportedDeviceSnapshotResult>.Fail("Reported-device snapshot is required and may contain at most 50 devices.", 400);

        return await _store.ExecuteSerializedAsync(command.EndpointId, async innerCt =>
        {
            var endpoint = await _store.GetEndpointAsync(command.EndpointId, innerCt);
            var expectedSource = endpoint?.ExecutionProfile == KioskExecutionProfile.FullEdge
                ? endpoint.FullEdgeRuntimeId
                : endpoint?.ControllerId;
            if (endpoint is null || endpoint.KioskId != command.KioskId || endpoint.Status != KioskExecutionEndpointStatus.Active ||
                expectedSource != command.SourceExecutorId)
                return ApiResult<ReportedDeviceSnapshotResult>.Fail("Active execution endpoint does not match the reported-device source.", 403);

            foreach (var deviceId in command.Devices.Where(item => item.DeviceId.HasValue).Select(item => item.DeviceId!.Value).Distinct())
            {
                if (await _store.GetDeviceByKioskIdAsync(endpoint.KioskId, deviceId, innerCt) is null)
                    return ApiResult<ReportedDeviceSnapshotResult>.Fail($"Device {deviceId:D} does not belong to the execution endpoint kiosk.", 400);
            }

            try
            {
                var previous = endpoint.ReportedDevices.ToArray();
                var now = DateTimeOffset.UtcNow;
                var disposition = endpoint.ApplyReportedDevicesSnapshot(
                    command.SourceExecutorId,
                    command.SnapshotRevision,
                    command.ObservedAt,
                    now,
                    command.Devices);
                if (disposition == ReportedDeviceSnapshotApplyDisposition.Applied)
                {
                    _store.RemoveReportedDevices(previous);
                    await _store.SaveChangesAsync(innerCt);
                }

                var result = Map(endpoint, disposition);
                var message = disposition switch
                {
                    ReportedDeviceSnapshotApplyDisposition.Applied => "Reported-device snapshot applied.",
                    ReportedDeviceSnapshotApplyDisposition.Stale => "Stale reported-device snapshot ignored.",
                    _ => "Reported-device snapshot already applied."
                };
                return ApiResult<ReportedDeviceSnapshotResult>.Success(result, message);
            }
            catch (DomainRuleException ex)
            {
                return ApiResult<ReportedDeviceSnapshotResult>.Fail(ex.Message, 409);
            }
        }, ct);
    }

    private static ReportedDeviceSnapshotResult Map(
        KioskExecutionEndpoint endpoint,
        ReportedDeviceSnapshotApplyDisposition disposition) => new()
        {
            EndpointId = endpoint.Id,
            SnapshotRevision = endpoint.ReportedDevicesSnapshotRevision ?? 0,
            Applied = disposition == ReportedDeviceSnapshotApplyDisposition.Applied,
            DuplicateOrStale = disposition is ReportedDeviceSnapshotApplyDisposition.Duplicate or ReportedDeviceSnapshotApplyDisposition.Stale,
            CloudReceivedAt = endpoint.ReportedDevicesReceivedAt,
            Devices = endpoint.ReportedDevices.OrderBy(item => item.SourceDeviceKey, StringComparer.OrdinalIgnoreCase)
            .Select(item => new ReportedDeviceResult
            {
                SourceDeviceKey = item.SourceDeviceKey,
                DeviceId = item.DeviceId,
                RuntimeTargetCode = item.RuntimeTargetCode,
                MachineModelCode = item.MachineModelCode
            }).ToArray()
        };
}
