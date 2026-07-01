using System.Text.Json;
using Application.Devices.Abstractions;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Domain.Common.Enums;
using Domain.Devices.Enums;
using Domain.Operations.Entities;
using Microsoft.Extensions.Options;

namespace Application.Devices.Commands;

public sealed class IngestLocalOperationLogCommandHandler
{
    private const int MaxPayloadCharacters = 16 * 1024;
    private readonly IEdgeTelemetryIngestionStore _store;
    private readonly EdgeTelemetryIngestionOptions _options;

    public IngestLocalOperationLogCommandHandler(
        IEdgeTelemetryIngestionStore store,
        IOptions<EdgeTelemetryIngestionOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public async Task<ApiResult<OperationLogIngestResult>> HandleAsync(
        IngestLocalOperationLogCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(command, DateTimeOffset.UtcNow);
        if (validationError is not null)
        {
            return ApiResult<OperationLogIngestResult>.Fail(validationError, 400);
        }

        var endpoint = await _store.GetEndpointAsync(command.EndpointId, cancellationToken);
        if (endpoint is null || endpoint.KioskId != command.KioskId ||
            endpoint.Status != KioskExecutionEndpointStatus.Active ||
            endpoint.CredentialBinding?.Status != ExecutionEndpointCredentialBindingStatus.Active)
        {
            return ApiResult<OperationLogIngestResult>.Fail("Execution endpoint authentication failed.", 401);
        }

        var boundNodeId = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.FullEdgeRuntimeId
            : endpoint.ControllerId;
        if (boundNodeId != command.OriginNodeId)
        {
            return ApiResult<OperationLogIngestResult>.Fail(
                "Operation log origin node does not match the execution endpoint identity.", 400);
        }

        return await _store.ExecuteOperationLogIngestionAsync(
            command.SourceEventId,
            ct => IngestLockedAsync(command, ct),
            cancellationToken);
    }

    private async Task<ApiResult<OperationLogIngestResult>> IngestLockedAsync(
        IngestLocalOperationLogCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await _store.GetOperationLogAsync(command.SourceEventId, cancellationToken);
        if (existing is not null)
        {
            return ApiResult<OperationLogIngestResult>.Success(ToResult(existing, true), "Operation log already ingested.");
        }

        if (command.DeviceId.HasValue)
        {
            var device = await _store.GetDeviceAsync(command.DeviceId.Value, cancellationToken);
            if (device?.KioskId != command.KioskId)
            {
                return ApiResult<OperationLogIngestResult>.Fail("Device does not belong to the reporting kiosk.", 400);
            }
        }

        if (command.OrderId.HasValue &&
            !await _store.OrderBelongsToKioskAsync(command.OrderId.Value, command.KioskId, cancellationToken))
        {
            return ApiResult<OperationLogIngestResult>.Fail("Order does not belong to the reporting kiosk.", 400);
        }

        var receivedAt = DateTimeOffset.UtcNow;
        var operationLog = new OperationLog
        {
            KioskId = command.KioskId,
            DeviceId = command.DeviceId,
            OrderId = command.OrderId,
            SourceEventId = command.SourceEventId,
            CorrelationId = command.CorrelationId,
            CausationId = command.CausationId,
            Action = command.Action.Trim(),
            Category = command.Category.Trim(),
            Severity = command.Severity,
            Message = command.Message.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(command.PayloadJson) ? null : command.PayloadJson,
            OccurredAt = command.OccurredAt,
            OriginNodeId = command.OriginNodeId,
            Version = 1,
            SyncedAt = receivedAt
        };
        await _store.AddOperationLogAsync(operationLog, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);
        return ApiResult<OperationLogIngestResult>.Success(ToResult(operationLog, false), "Operation log ingested.", 201);
    }

    private string? Validate(IngestLocalOperationLogCommand command, DateTimeOffset receivedAt)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty || command.OriginNodeId == Guid.Empty ||
            command.SourceEventId == Guid.Empty || command.OccurredAt == default ||
            string.IsNullOrWhiteSpace(command.Action) || string.IsNullOrWhiteSpace(command.Category) ||
            string.IsNullOrWhiteSpace(command.Message))
        {
            return "Kiosk, endpoint, origin node, source event, action, category, message, and occurred timestamp are required.";
        }

        if (command.Action.Trim().Length > 500 || command.Category.Trim().Length > 500 ||
            command.Message.Trim().Length > 500)
        {
            return "Operation log action, category, or message exceeds the allowed length.";
        }

        if (!Enum.IsDefined(command.Severity))
        {
            return "Operation log severity is invalid.";
        }

        if (command.OccurredAt > receivedAt.AddSeconds(_options.MaxFutureClockSkewSeconds))
        {
            return "Operation log timestamp cannot exceed the allowed future clock skew.";
        }

        if (!string.IsNullOrWhiteSpace(command.PayloadJson))
        {
            if (command.PayloadJson.Length > MaxPayloadCharacters)
            {
                return "Operation log payload exceeds 16384 characters.";
            }

            try
            {
                using var _ = JsonDocument.Parse(command.PayloadJson);
            }
            catch (JsonException)
            {
                return "Operation log payload must be valid JSON.";
            }
        }

        return null;
    }

    private static OperationLogIngestResult ToResult(OperationLog log, bool duplicate) => new()
    {
        OperationLogId = log.Id,
        SourceEventId = log.SourceEventId!.Value,
        ReceivedAt = log.SyncedAt ?? log.CreatedAt,
        Duplicate = duplicate
    };
}
