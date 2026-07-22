using Application.Devices.Telemetry;
using Domain.Devices.ExecutionEndpoints;
using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Connectivity.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Catalog;
using Microsoft.Extensions.Options;
using Domain.Devices.ExecutionEndpoints.Projections;
using Application.Devices.Connectivity.Rules;

namespace Application.Devices.Connectivity.Commands;
public sealed class IngestExecutionReadinessCommandHandler
{
    private readonly IExecutionReadinessStore _store;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly EdgeTelemetryIngestionOptions _options;
    public IngestExecutionReadinessCommandHandler(IExecutionReadinessStore store, IRealtimeNotificationPublisher publisher, IOptions<EdgeTelemetryIngestionOptions> options)
    { _store = store; _publisher = publisher; _options = options.Value; }

    public async Task<ApiResult<ExecutionReadinessResult>> HandleAsync(IngestExecutionReadinessCommand command, CancellationToken ct = default)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty || command.SourceExecutorId == Guid.Empty || command.StateRevision <= 0)
            return ApiResult<ExecutionReadinessResult>.Fail("Readiness source identity and positive revision are required.", 400);
        if (command.ExecutorReportedAt > DateTimeOffset.UtcNow.AddSeconds(_options.MaxFutureClockSkewSeconds))
            return ApiResult<ExecutionReadinessResult>.Fail("Readiness timestamp is too far in the future.", 400);
        if (command.LocalPersistenceHealth is null)
            return ApiResult<ExecutionReadinessResult>.Fail("Local persistence health is required.", 400);
        var persistenceError = LocalPersistenceReadinessRules.Validate(command.LocalPersistenceHealth);
        if (persistenceError is not null)
            return ApiResult<ExecutionReadinessResult>.Fail(persistenceError, 400);
        var effective = LocalPersistenceReadinessRules.Apply(
            command.LocalPersistenceHealth,
            command.Readiness,
            command.FaultCode);
        var codes = command.Capabilities.Select(x => x.CapabilityCode?.Trim().ToUpperInvariant()).ToArray();
        if (codes.Any(string.IsNullOrWhiteSpace) || codes.Distinct(StringComparer.Ordinal).Count() != codes.Length)
            return ApiResult<ExecutionReadinessResult>.Fail("Capability codes must be non-empty and unique.", 400);

        var result = await _store.ExecuteSerializedAsync(command.EndpointId, async innerCt =>
        {
            var endpoint = await _store.GetEndpointAsync(command.EndpointId, innerCt);
            var expected = endpoint?.ExecutionProfile == KioskExecutionProfile.FullEdge ? endpoint.FullEdgeRuntimeId : endpoint?.ControllerId;
            if (endpoint is null || endpoint.KioskId != command.KioskId || expected != command.SourceExecutorId || endpoint.Status != KioskExecutionEndpointStatus.Active)
                return ApiResult<ExecutionReadinessResult>.Fail("Active execution endpoint does not match the readiness source.", 403);
            var current = await _store.GetProjectionAsync(endpoint.Id, true, innerCt);
            if (current is not null && command.StateRevision < current.StateRevision)
                return ApiResult<ExecutionReadinessResult>.Success(Map(current, false, true), "Stale readiness revision ignored.");
            if (current is not null && command.StateRevision == current.StateRevision)
            {
                if (!Matches(current, command))
                    return ApiResult<ExecutionReadinessResult>.Fail("Readiness revision was reused with different content.", 409);
                return ApiResult<ExecutionReadinessResult>.Success(Map(current, false, true), "Readiness revision already applied.");
            }

            var now = DateTimeOffset.UtcNow;
            if (current is null)
            {
                current = ExecutionEndpointReadinessProjection.Create(command.KioskId, endpoint.Id, command.SourceExecutorId,
                    command.StateRevision, effective.Readiness, command.Activity, command.Safety, command.CurrentCommandId,
                    command.PhysicalOutputState, effective.FaultCode, command.ExecutorReportedAt, now);
                await _store.AddProjectionAsync(current, innerCt);
            }
            else current.Apply(command.StateRevision, effective.Readiness, command.Activity, command.Safety,
                command.CurrentCommandId, command.PhysicalOutputState, effective.FaultCode, command.ExecutorReportedAt, now);

            _store.ReplaceCapabilities(current, command.Capabilities.Select(x => new ExecutionEndpointCapabilityProjection
            {
                CapabilityCode = x.CapabilityCode.Trim().ToUpperInvariant(), WorkcellCode = string.IsNullOrWhiteSpace(x.WorkcellCode) ? null : x.WorkcellCode.Trim().ToUpperInvariant(),
                IsAvailable = x.IsAvailable, UnavailableReason = string.IsNullOrWhiteSpace(x.UnavailableReason) ? null : x.UnavailableReason.Trim()
            }).ToArray());
            await _store.SaveChangesAsync(innerCt);
            return ApiResult<ExecutionReadinessResult>.Success(Map(current, true, false), "Execution readiness applied.");
        }, ct);

        if (result.Succeeded && result.Data!.Applied)
            await _publisher.PublishExecutionReadinessChangedAsync(new ExecutionReadinessChangedEvent
            {
                KioskId = command.KioskId, EndpointId = command.EndpointId, StateRevision = command.StateRevision,
                Readiness = effective.Readiness.ToString(), Activity = command.Activity.ToString(), Safety = command.Safety.ToString(),
                OccurredAt = DateTimeOffset.UtcNow
            }, ct);
        return result;
    }

    private static ExecutionReadinessResult Map(ExecutionEndpointReadinessProjection x, bool applied, bool duplicate) => new()
    { EndpointId = x.KioskExecutionEndpointId, StateRevision = x.StateRevision, Applied = applied, DuplicateOrStale = duplicate,
      Readiness = x.Readiness.ToString(), Activity = x.Activity.ToString(), Safety = x.Safety.ToString(), CloudReceivedAt = x.CloudReceivedAt };

    private static bool Matches(ExecutionEndpointReadinessProjection current, IngestExecutionReadinessCommand command)
    {
        var effective = LocalPersistenceReadinessRules.Apply(
            command.LocalPersistenceHealth,
            command.Readiness,
            command.FaultCode);
        if (current.Readiness != effective.Readiness || current.Activity != command.Activity || current.Safety != command.Safety ||
            current.CurrentCommandId != command.CurrentCommandId || current.PhysicalOutputState != command.PhysicalOutputState ||
            !string.Equals(current.FaultCode, effective.FaultCode, StringComparison.Ordinal)) return false;
        var expected = command.Capabilities.OrderBy(x => x.CapabilityCode, StringComparer.OrdinalIgnoreCase).ToArray();
        var actual = current.Capabilities.OrderBy(x => x.CapabilityCode, StringComparer.OrdinalIgnoreCase).ToArray();
        return expected.Length == actual.Length && expected.Zip(actual).All(pair =>
            string.Equals(pair.First.CapabilityCode.Trim(), pair.Second.CapabilityCode, StringComparison.OrdinalIgnoreCase) &&
            pair.First.IsAvailable == pair.Second.IsAvailable &&
            string.Equals(pair.First.WorkcellCode?.Trim(), pair.Second.WorkcellCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(pair.First.UnavailableReason?.Trim(), pair.Second.UnavailableReason, StringComparison.Ordinal));
    }
}
