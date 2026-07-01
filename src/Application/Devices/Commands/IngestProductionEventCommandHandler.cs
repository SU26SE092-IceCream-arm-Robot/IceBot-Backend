using Domain.Sync.Ingestion;
using System.Text.Json;
using Application.Devices.Abstractions;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Enums;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Microsoft.Extensions.Options;

namespace Application.Devices.Commands;

public sealed class IngestProductionEventCommandHandler
{
    private readonly IEdgeTelemetryIngestionStore _telemetryStore;
    private readonly IProductionEventSyncStore _syncStore;
    private readonly EdgeTelemetryIngestionOptions _options;

    public IngestProductionEventCommandHandler(
        IEdgeTelemetryIngestionStore telemetryStore,
        IProductionEventSyncStore syncStore,
        IOptions<EdgeTelemetryIngestionOptions> options)
    {
        _telemetryStore = telemetryStore;
        _syncStore = syncStore;
        _options = options.Value;
    }

    public async Task<ApiResult<ProductionEventSyncResult>> HandleAsync(
        IngestProductionEventCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty ||
            command.SourceExecutorId == Guid.Empty || command.EventId == Guid.Empty ||
            command.SequenceNumber <= 0 || string.IsNullOrWhiteSpace(command.EventType) ||
            command.EventType.Length > 100 || command.SchemaVersion <= 0 || !IsValidJson(command.PayloadJson))
        {
            return ApiResult<ProductionEventSyncResult>.Fail("Production event identity, sequence, type, and schema version are required.", 400);
        }

        if (command.EdgeCreatedAt > DateTimeOffset.UtcNow.AddSeconds(_options.MaxFutureClockSkewSeconds))
        {
            return ApiResult<ProductionEventSyncResult>.Fail("Production event timestamp is too far in the future.", 400);
        }

        var endpoint = await _telemetryStore.GetEndpointAsync(command.EndpointId, cancellationToken);
        var expectedExecutorId = endpoint?.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.FullEdgeRuntimeId
            : endpoint?.ControllerId;
        if (endpoint is null || endpoint.KioskId != command.KioskId || expectedExecutorId != command.SourceExecutorId)
        {
            return ApiResult<ProductionEventSyncResult>.Fail("Execution endpoint does not match the production event source.", 403);
        }

        return await _syncStore.ExecuteHistoryIngestionAsync(command.SourceExecutorId, async ct =>
        {
            var expectedPayloadJson = BuildStoredPayload(command);
            var checkpoint = await _syncStore.GetCheckpointAsync(command.SourceExecutorId, tracked: true, ct);
            if (checkpoint is not null && command.SequenceNumber <= checkpoint.LastContiguousSequenceNumber)
            {
                return Success(command.EventId, duplicate: true, checkpoint.LastContiguousSequenceNumber);
            }

            var existingById = await _syncStore.GetEventByIdAsync(command.SourceExecutorId, command.EventId, ct);
            if (existingById is not null)
            {
                if (existingById.SourceNodeId != command.SourceExecutorId ||
                    existingById.SequenceNumber != command.SequenceNumber ||
                    existingById.KioskId != command.KioskId ||
                    existingById.AggregateId != command.ProductionJobId ||
                    !string.Equals(existingById.EventType, command.EventType.Trim(), StringComparison.Ordinal) ||
                    !string.Equals(existingById.PayloadJson, expectedPayloadJson, StringComparison.Ordinal))
                {
                    return ApiResult<ProductionEventSyncResult>.Fail("Production event id was reused with a different identity or payload envelope.", 409);
                }

                checkpoint ??= await CreateCheckpointAsync(command, ct);
                await AdvanceCheckpointAsync(command.SourceExecutorId, checkpoint, ct);
                await _syncStore.SaveChangesAsync(ct);
                return Success(command.EventId, duplicate: true, checkpoint.LastContiguousSequenceNumber);
            }

            var existingBySequence = await _syncStore.GetEventBySequenceAsync(command.SourceExecutorId, command.SequenceNumber, ct);
            if (existingBySequence is not null)
            {
                return ApiResult<ProductionEventSyncResult>.Fail("Production event sequence was already assigned to another event.", 409);
            }

            var now = DateTimeOffset.UtcNow;
            await _syncStore.AddEventAsync(new SyncEventInbox
            {
                EventId = command.EventId,
                KioskId = command.KioskId,
                SourceNodeId = command.SourceExecutorId,
                SequenceNumber = command.SequenceNumber,
                EventType = command.EventType.Trim(),
                AggregateType = "ProductionJob",
                AggregateId = command.ProductionJobId,
                PayloadJson = expectedPayloadJson,
                Status = SyncEventStatus.Processed,
                OccurredAt = command.EdgeCreatedAt,
                ReceivedAt = now,
                ProcessedAt = now
            }, ct);
            await _syncStore.SaveChangesAsync(ct);

            if (checkpoint is null)
            {
                checkpoint = await CreateCheckpointAsync(command, ct);
            }

            await AdvanceCheckpointAsync(command.SourceExecutorId, checkpoint, ct);
            await _syncStore.SaveChangesAsync(ct);
            return Success(command.EventId, duplicate: false, checkpoint.LastContiguousSequenceNumber);
        }, cancellationToken);
    }

    private static ApiResult<ProductionEventSyncResult> Success(Guid eventId, bool duplicate, long checkpoint) =>
        ApiResult<ProductionEventSyncResult>.Success(new ProductionEventSyncResult
        {
            EventId = eventId,
            Duplicate = duplicate,
            AcknowledgedSequenceNumber = checkpoint
        }, duplicate ? "Production event already accepted." : "Production event accepted.");

    private async Task<ProductionEventCheckpoint> CreateCheckpointAsync(
        IngestProductionEventCommand command,
        CancellationToken cancellationToken)
    {
        var checkpoint = ProductionEventCheckpoint.Create(
            command.KioskId, command.EndpointId, command.SourceExecutorId);
        await _syncStore.AddCheckpointAsync(checkpoint, cancellationToken);
        return checkpoint;
    }

    private async Task AdvanceCheckpointAsync(
        Guid sourceExecutorId,
        ProductionEventCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        var candidates = await _syncStore.ListContiguousCandidatesAsync(
            sourceExecutorId, checkpoint.LastContiguousSequenceNumber, cancellationToken);
        foreach (var candidate in candidates)
        {
            if (candidate.SequenceNumber != checkpoint.LastContiguousSequenceNumber + 1)
            {
                break;
            }
            checkpoint.AdvanceTo(candidate.SequenceNumber.Value, candidate.EventId);
        }
    }

    private static JsonElement? ParsePayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }
        return JsonSerializer.Deserialize<JsonElement>(payloadJson);
    }

    private static string BuildStoredPayload(IngestProductionEventCommand command) =>
        JsonSerializer.Serialize(new
        {
            schemaVersion = command.SchemaVersion,
            orderId = command.OrderId,
            sourceCommandId = command.SourceCommandId,
            productionJobId = command.ProductionJobId,
            payload = ParsePayload(command.PayloadJson)
        });

    private static bool IsValidJson(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return true;
        }
        try
        {
            using var _ = JsonDocument.Parse(payloadJson);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
