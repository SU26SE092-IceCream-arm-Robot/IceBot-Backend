using Application.Devices.Abstractions;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Enums;

namespace Application.Devices.Queries;

public sealed class GetProductionEventCheckpointQueryHandler
{
    private readonly IEdgeTelemetryIngestionStore _telemetryStore;
    private readonly IProductionEventSyncStore _syncStore;

    public GetProductionEventCheckpointQueryHandler(
        IEdgeTelemetryIngestionStore telemetryStore,
        IProductionEventSyncStore syncStore)
    {
        _telemetryStore = telemetryStore;
        _syncStore = syncStore;
    }

    public async Task<ApiResult<ProductionEventCheckpointResult>> HandleAsync(
        GetProductionEventCheckpointQuery query,
        CancellationToken cancellationToken = default)
    {
        var endpoint = await _telemetryStore.GetEndpointAsync(query.EndpointId, cancellationToken);
        var expectedExecutorId = endpoint?.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.FullEdgeRuntimeId
            : endpoint?.ControllerId;
        if (endpoint is null || endpoint.KioskId != query.KioskId || expectedExecutorId != query.SourceExecutorId)
        {
            return ApiResult<ProductionEventCheckpointResult>.Fail("Execution endpoint does not match the checkpoint source.", 403);
        }

        var checkpoint = await _syncStore.GetCheckpointAsync(query.SourceExecutorId, tracked: false, cancellationToken);
        return ApiResult<ProductionEventCheckpointResult>.Success(new ProductionEventCheckpointResult
        {
            SourceExecutorId = query.SourceExecutorId,
            LastContiguousSequenceNumber = checkpoint?.LastContiguousSequenceNumber ?? 0,
            LastContiguousEventId = checkpoint?.LastContiguousEventId,
            UpdatedAt = checkpoint?.UpdatedAt ?? checkpoint?.CreatedAt
        });
    }
}
