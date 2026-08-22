using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.Inventory.Abstractions;
using Application.Inventory.Support;
using Application.Shared.Wrappers;
using Domain.Devices.ExecutionEndpoints;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;

namespace Application.Inventory.Observations;

public sealed class IngestInventorySensorObservationsCommandHandler(
    IInventorySensorObservationStore observations,
    IExecutionEndpointTransportAuthStore endpoints,
    IRealtimeNotificationPublisher publisher)
{
    private const int MaximumBatchSize = 100;
    private const int MaximumSensorPayloadLength = 16_384;
    private const int MaxFutureClockSkewSeconds = 300;

    public async Task<ApiResult<InventorySensorObservationIngestResult>> HandleAsync(
        IngestInventorySensorObservationsCommand command,
        CancellationToken cancellationToken = default)
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var validationError = Validate(command, receivedAt);
        if (validationError is not null)
            return ApiResult<InventorySensorObservationIngestResult>.Fail(validationError, 400);

        var endpoint = await endpoints.GetEndpointAsync(command.EndpointId, cancellationToken);
        if (!IsUsable(endpoint, command))
            return ApiResult<InventorySensorObservationIngestResult>.Fail("Execution endpoint authentication failed.", 401);

        var notifications = new List<InventoryChangedEvent>();
        try
        {
            var result = await observations.ExecuteObservationIngestionAsync(
                command.SourceExecutorId,
                command.Observations.Select(item => item.SourceEventId).ToArray(),
                command.Observations.Select(item => item.IngredientDispenserStateId).ToArray(),
                ct => IngestLockedAsync(command, receivedAt, notifications, ct),
                cancellationToken);

            if (result.Succeeded)
            {
                foreach (var notification in notifications)
                    await publisher.PublishInventoryChangedAsync(notification, cancellationToken);
            }

            return result;
        }
        catch (Domain.Common.DomainRuleException ex)
        {
            return ApiResult<InventorySensorObservationIngestResult>.Fail(ex.Message, 409);
        }
    }

    private async Task<ApiResult<InventorySensorObservationIngestResult>> IngestLockedAsync(
        IngestInventorySensorObservationsCommand command,
        DateTimeOffset receivedAt,
        ICollection<InventoryChangedEvent> notifications,
        CancellationToken cancellationToken)
    {
        var applied = 0;
        var duplicate = 0;
        var outOfOrder = 0;

        foreach (var input in command.Observations.OrderBy(item => item.IngredientDispenserStateId).ThenBy(item => item.ObservationSequence))
        {
            var existing = await observations.GetObservationBySourceEventAsync(
                command.SourceExecutorId, input.SourceEventId, cancellationToken);
            if (existing is not null)
            {
                if (existing.IngredientDispenserStateId != input.IngredientDispenserStateId ||
                    existing.DeviceId != input.DeviceId ||
                    existing.ObservationSequence != input.ObservationSequence ||
                    existing.ObservedLevelStatus != input.ObservedLevelStatus)
                {
                    return ApiResult<InventorySensorObservationIngestResult>.Fail(
                        "Inventory observation source event id was reused with different evidence.", 409);
                }

                duplicate++;
                continue;
            }

            var state = await observations.GetDispenserStateAsync(input.IngredientDispenserStateId, cancellationToken);
            if (state is null || state.KioskId != command.KioskId || state.DeviceId != input.DeviceId)
                return ApiResult<InventorySensorObservationIngestResult>.Fail("Dispenser state was not found for this endpoint kiosk and device.", 404);
            if (!state.IsActive)
                return ApiResult<InventorySensorObservationIngestResult>.Fail("Retired dispenser state cannot accept sensor observations.", 409);

            var latestSequence = await observations.GetLatestAppliedSequenceAsync(
                command.SourceExecutorId, state.Id, cancellationToken);
            var stale = latestSequence.HasValue && input.ObservationSequence <= latestSequence.Value ||
                        state.LastSensorObservedAt.HasValue && input.ObservedAt <= state.LastSensorObservedAt.Value;

            var payloadJson = input.SensorPayload?.GetRawText();
            decimal? derivedEstimate = null;
            var disposition = InventorySensorObservationDisposition.OutOfOrder;
            if (!stale)
            {
                DispenserLevelQuantityProfileContract.TryResolveEstimatedQuantity(
                    state.LevelToQuantityProfileJson,
                    input.ObservedLevelStatus,
                    out derivedEstimate);
                var previousContribution = state.LastObservedEstimatedQuantity;
                var establishesRebaseline = state.SensorRebaselineRequired;
                state.RecordSensorLevel(input.ObservedLevelStatus, input.ObservedAt, payloadJson, derivedEstimate);
                if (establishesRebaseline)
                {
                    state.ConsumeSensorRebaseline();
                }
                var balance = state.KioskIngredientInventory;
                disposition = balance is null
                    ? InventorySensorObservationDisposition.Unbound
                    : balance.TrackingMode == InventoryTrackingMode.ManualEstimate
                        ? InventorySensorObservationDisposition.EvidenceOnly
                        : InventorySensorObservationDisposition.Applied;
                if (!establishesRebaseline && balance is not null &&
                    balance.TrackingMode is (InventoryTrackingMode.SensorAssisted or InventoryTrackingMode.SensorRequired) &&
                    derivedEstimate.HasValue)
                {
                    await observations.AcquireKioskIngredientInventoryMutationLockAsync(balance.Id, cancellationToken);
                    var before = balance.EstimatedQuantity;
                    balance.ReconcileSensorDelta(derivedEstimate.Value, previousContribution, receivedAt);
                    var delta = derivedEstimate.Value - (previousContribution ?? derivedEstimate.Value);
                    if (delta != 0 && before.HasValue && balance.EstimatedQuantity.HasValue)
                    {
                        await observations.AddStockMovementAsync(StockMovement.CreateForKioskInventory(
                            balance.Id,
                            balance.OrganizationId,
                            balance.StoreId,
                            balance.KioskId,
                            balance.IngredientId,
                            "SensorReconciliation",
                            delta,
                            before,
                            balance.EstimatedQuantity,
                            balance.Unit,
                            receivedAt,
                            "SENSOR_DELTA",
                            "InventorySensorObservation",
                            input.SourceEventId,
                            input.SourceEventId,
                            isEstimated: true,
                            ingredientDispenserStateId: state.Id), cancellationToken);
                    }
                }
                applied++;
                notifications.Add(new InventoryChangedEvent
                {
                    DispenserStateId = state.Id,
                    KioskId = state.KioskId ?? Guid.Empty,
                    OrganizationId = state.Kiosk?.OrganizationId,
                    StoreId = state.Kiosk?.StoreId,
                    IngredientName = state.Ingredient.Name,
                    EstimatedQuantity = state.EstimatedQuantity,
                    Unit = state.Unit,
                    Status = state.CurrentLevelStatus.ToString(),
                    UpdatedAt = state.LastMeasuredAt,
                    Version = 1
                });
            }
            else
            {
                outOfOrder++;
            }

            await observations.AddObservationAsync(new InventorySensorObservation
            {
                Id = Guid.NewGuid(),
                KioskExecutionEndpointId = command.EndpointId,
                SourceExecutorId = command.SourceExecutorId,
                SourceEventId = input.SourceEventId,
                IngredientDispenserStateId = state.Id,
                DeviceId = state.DeviceId,
                IngredientId = state.IngredientId,
                ObservationSequence = input.ObservationSequence,
                ObservedLevelStatus = input.ObservedLevelStatus,
                DerivedEstimatedQuantity = derivedEstimate,
                ObservedAt = input.ObservedAt,
                CloudReceivedAt = receivedAt,
                Disposition = stale
                    ? InventorySensorObservationDisposition.OutOfOrder
                    : disposition,
                SensorPayloadJson = payloadJson,
                OriginNodeId = command.SourceExecutorId,
                Version = input.ObservationSequence,
                CreatedAt = receivedAt
            }, cancellationToken);
        }

        await observations.SaveChangesAsync(cancellationToken);
        return ApiResult<InventorySensorObservationIngestResult>.Success(
            new InventorySensorObservationIngestResult
            {
                AppliedCount = applied,
                DuplicateCount = duplicate,
                OutOfOrderCount = outOfOrder
            },
            "Inventory sensor observations ingested.");
    }

    private static bool IsUsable(KioskExecutionEndpoint? endpoint, IngestInventorySensorObservationsCommand command) =>
        endpoint is not null && endpoint.KioskId == command.KioskId &&
        endpoint.Status == KioskExecutionEndpointStatus.Active &&
        endpoint.CredentialBinding?.Status == ExecutionEndpointCredentialBindingStatus.Active &&
        (endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.FullEdgeRuntimeId
            : endpoint.ControllerId) == command.SourceExecutorId;

    private static string? Validate(IngestInventorySensorObservationsCommand command, DateTimeOffset receivedAt)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty || command.SourceExecutorId == Guid.Empty)
            return "Kiosk, endpoint, and source executor are required.";
        if (command.Observations.Count is < 1 or > MaximumBatchSize)
            return $"Inventory sensor observations must contain between 1 and {MaximumBatchSize} items.";
        if (command.Observations.Select(item => item.SourceEventId).Distinct().Count() != command.Observations.Count)
            return "Inventory sensor observation source event ids must be unique within a batch.";
        if (command.Observations.GroupBy(item => new { item.IngredientDispenserStateId, item.ObservationSequence })
            .Any(group => group.Count() > 1))
            return "Inventory observation sequences must be unique per dispenser state within a batch.";

        foreach (var item in command.Observations)
        {
            if (item.SourceEventId == Guid.Empty || item.IngredientDispenserStateId == Guid.Empty || item.DeviceId == Guid.Empty ||
                item.ObservationSequence <= 0 || item.ObservedAt == default)
                return "Each inventory observation requires source event, dispenser state, device, positive sequence, and observed timestamp.";
            if (item.ObservedLevelStatus is not (IngredientLevelStatus.Low or IngredientLevelStatus.Medium or IngredientLevelStatus.Full))
                return "Inventory sensor observations support only Low, Medium, or Full levels.";
            if (item.ObservedAt > receivedAt.AddSeconds(MaxFutureClockSkewSeconds))
                return "Inventory observation timestamp cannot exceed the allowed future clock skew.";
            if (item.SensorPayload is { } payload && payload.GetRawText().Length > MaximumSensorPayloadLength)
                return "Inventory observation sensor payload exceeds the supported size.";
        }

        return null;
    }
}
