using Domain.Devices.ExecutionEndpoints;
using Domain.Sync.Ingestion;
using System.Text.Json;
using Application.Devices.Telemetry;
using Application.Devices.Telemetry.Abstractions;
using Application.Sync.Ingestion.Abstractions;
using Application.Sync.Ingestion.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Catalog;
using Domain.Sync.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Sync.Ingestion.Commands;

public sealed class IngestEdgeStateSummariesCommandHandler
{
    private const int MaxSummaryCount = 50;
    private readonly IEdgeTelemetryIngestionStore _telemetryStore;
    private readonly IProductionEventSyncStore _syncStore;
    private readonly EdgeTelemetryIngestionOptions _options;
    private readonly ILogger<IngestEdgeStateSummariesCommandHandler> _logger;

    public IngestEdgeStateSummariesCommandHandler(
        IEdgeTelemetryIngestionStore telemetryStore,
        IProductionEventSyncStore syncStore,
        IOptions<EdgeTelemetryIngestionOptions> options,
        ILogger<IngestEdgeStateSummariesCommandHandler> logger)
    {
        _telemetryStore = telemetryStore;
        _syncStore = syncStore;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ApiResult<EdgeStateSummarySyncResult>> HandleAsync(
        IngestEdgeStateSummariesCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty || command.SourceExecutorId == Guid.Empty ||
            command.Summaries.Count is 0 or > MaxSummaryCount)
        {
            return ApiResult<EdgeStateSummarySyncResult>.Fail($"Summary identity and between 1 and {MaxSummaryCount} summaries are required.", 400);
        }

        if (command.Summaries.Any(item => string.IsNullOrWhiteSpace(item.SummaryKind)) ||
            command.Summaries.Select(item => item.SummaryKind.Trim()).Distinct(StringComparer.Ordinal).Count() != command.Summaries.Count)
        {
            return ApiResult<EdgeStateSummarySyncResult>.Fail("Summary kinds must be unique within the request.", 400);
        }

        var endpoint = await _telemetryStore.GetEndpointAsync(command.EndpointId, cancellationToken);
        var expectedExecutorId = endpoint?.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.FullEdgeRuntimeId
            : endpoint?.ControllerId;
        if (endpoint is null || endpoint.KioskId != command.KioskId || expectedExecutorId != command.SourceExecutorId)
        {
            return ApiResult<EdgeStateSummarySyncResult>.Fail("Execution endpoint does not match the state summary source.", 403);
        }

        var results = new List<EdgeStateSummarySyncItemResult>(command.Summaries.Count);
        foreach (var item in command.Summaries)
        {
            try
            {
                results.Add(await ApplyAsync(command, item, cancellationToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "State summary {SummaryKind} revision {StateRevision} failed.",
                    item.SummaryKind, item.StateRevision);
                results.Add(Result(item.SummaryKind, item.StateRevision, "Failed", 500, "State summary processing failed."));
            }
        }

        var rejected = results.Count(item => item.Status is "Rejected" or "Failed");
        var data = new EdgeStateSummarySyncResult
        {
            AppliedCount = results.Count(item => item.Status == "Applied"),
            DuplicateCount = results.Count(item => item.Status == "Duplicate"),
            StaleCount = results.Count(item => item.Status == "Stale"),
            RejectedCount = rejected,
            Items = results
        };
        return ApiResult<EdgeStateSummarySyncResult>.Success(
            data,
            rejected == 0 ? "State summaries processed." : "State summaries processed with item failures.",
            rejected == 0 ? 200 : 207);
    }

    private async Task<EdgeStateSummarySyncItemResult> ApplyAsync(
        IngestEdgeStateSummariesCommand command,
        EdgeStateSummaryItem item,
        CancellationToken cancellationToken)
    {
        var kind = item.SummaryKind.Trim();
        if (string.IsNullOrWhiteSpace(kind) || kind.Length > 100 || item.StateRevision <= 0 ||
            item.SummarySchemaVersion <= 0 || string.IsNullOrWhiteSpace(item.PayloadJson) ||
            !IsValidJson(item.PayloadJson) ||
            item.EdgeCreatedAt > DateTimeOffset.UtcNow.AddSeconds(_options.MaxFutureClockSkewSeconds))
        {
            return Result(kind, item.StateRevision, "Rejected", 400, "Summary kind, revision, schema version, timestamp, or payload is invalid.");
        }

        return await _syncStore.ExecuteSummaryIngestionAsync(command.SourceExecutorId, kind, async ct =>
        {
            var current = await _syncStore.GetStateSummaryAsync(command.SourceExecutorId, kind, tracked: true, ct);
            if (current is not null && item.StateRevision < current.StateRevision)
            {
                return Result(kind, item.StateRevision, "Stale", 200, "Older state revision ignored.");
            }
            if (current is not null && item.StateRevision == current.StateRevision)
            {
                var identical = current.SummarySchemaVersion == item.SummarySchemaVersion &&
                                current.EdgeCreatedAt == item.EdgeCreatedAt &&
                                string.Equals(current.PayloadJson, item.PayloadJson, StringComparison.Ordinal);
                return identical
                    ? Result(kind, item.StateRevision, "Duplicate", 200, "State revision already applied.")
                    : Result(kind, item.StateRevision, "Rejected", 409, "State revision was reused with different content.");
            }

            var now = DateTimeOffset.UtcNow;
            if (current is null)
            {
                await _syncStore.AddStateSummaryAsync(EdgeStateSummary.Create(
                    command.KioskId, command.EndpointId, command.SourceExecutorId, kind,
                    item.StateRevision, item.SummarySchemaVersion, item.EdgeCreatedAt, now, item.PayloadJson), ct);
            }
            else
            {
                current.ApplyNewerRevision(
                    item.StateRevision, item.SummarySchemaVersion, item.EdgeCreatedAt, now, item.PayloadJson);
            }
            await _syncStore.SaveChangesAsync(ct);
            return Result(kind, item.StateRevision, "Applied", 200, "State summary applied.");
        }, cancellationToken);
    }

    private static EdgeStateSummarySyncItemResult Result(
        string kind, long revision, string status, int statusCode, string message) => new()
        {
            SummaryKind = kind,
            StateRevision = revision,
            Status = status,
            StatusCode = statusCode,
            Message = message
        };

    private static bool IsValidJson(string payloadJson)
    {
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
