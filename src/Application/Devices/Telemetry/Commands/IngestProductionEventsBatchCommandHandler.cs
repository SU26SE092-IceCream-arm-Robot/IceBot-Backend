using Application.Devices.Telemetry.Results;
using Application.Shared.Wrappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Devices.Telemetry.Commands;

public sealed class IngestProductionEventsBatchCommandHandler
{
    private readonly IngestProductionEventCommandHandler _eventHandler;
    private readonly EdgeTelemetryIngestionOptions _options;
    private readonly ILogger<IngestProductionEventsBatchCommandHandler> _logger;

    public IngestProductionEventsBatchCommandHandler(
        IngestProductionEventCommandHandler eventHandler,
        IOptions<EdgeTelemetryIngestionOptions> options,
        ILogger<IngestProductionEventsBatchCommandHandler> logger)
    {
        _eventHandler = eventHandler;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ApiResult<BatchEventSyncResult>> HandleAsync(
        IngestProductionEventsBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty || command.SourceExecutorId == Guid.Empty ||
            command.Events.Count is 0 || command.Events.Count > _options.MaxBatchEventCount)
        {
            return ApiResult<BatchEventSyncResult>.Fail(
                $"Production event batch identity and between 1 and {_options.MaxBatchEventCount} events are required.", 400);
        }

        if (command.Events.Any(item => item.EventId == Guid.Empty) ||
            command.Events.Select(item => item.EventId).Distinct().Count() != command.Events.Count)
        {
            return ApiResult<BatchEventSyncResult>.Fail("Production event ids must be non-empty and unique within the request.", 400);
        }

        var items = new List<BatchEventSyncItemResult>(command.Events.Count);
        foreach (var item in command.Events)
        {
            try
            {
                var result = await _eventHandler.HandleAsync(new IngestProductionEventCommand
                {
                    KioskId = command.KioskId,
                    EndpointId = command.EndpointId,
                    SourceExecutorId = command.SourceExecutorId,
                    EventId = item.EventId,
                    SequenceNumber = item.SequenceNumber,
                    EventType = item.EventType,
                    SchemaVersion = item.SchemaVersion,
                    EdgeCreatedAt = item.EdgeCreatedAt,
                    OrderId = item.OrderId,
                    SourceCommandId = item.SourceCommandId,
                    ProductionJobId = item.ProductionJobId,
                    PayloadJson = item.PayloadJson
                }, cancellationToken);

                items.Add(new BatchEventSyncItemResult
                {
                    EventId = item.EventId,
                    EventType = item.EventType,
                    Status = !result.Succeeded ? "Rejected" : result.Data!.Duplicate ? "Duplicate" : "Accepted",
                    StatusCode = result.StatusCode,
                    Message = result.Message,
                    ResourceId = result.Data?.EventId,
                    AcknowledgedSequenceNumber = result.Data?.AcknowledgedSequenceNumber
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Production event {EventId} failed.", item.EventId);
                items.Add(new BatchEventSyncItemResult
                {
                    EventId = item.EventId,
                    EventType = item.EventType,
                    Status = "Failed",
                    StatusCode = 500,
                    Message = "Production event processing failed."
                });
            }
        }

        var rejected = items.Count(item => item.Status is "Rejected" or "Failed");
        return ApiResult<BatchEventSyncResult>.Success(new BatchEventSyncResult
        {
            AcceptedCount = items.Count(item => item.Status == "Accepted"),
            DuplicateCount = items.Count(item => item.Status == "Duplicate"),
            RejectedCount = rejected,
            Items = items
        }, rejected == 0 ? "Production events processed." : "Production events processed with item failures.",
            rejected == 0 ? 200 : 207);
    }
}
