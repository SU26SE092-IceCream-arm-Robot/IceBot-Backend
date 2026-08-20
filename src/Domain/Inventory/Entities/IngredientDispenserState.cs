using Domain.Catalog.Entities;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Inventory.Enums;
using Domain.Tenants.Entities;

namespace Domain.Inventory.Entities;

public partial class IngredientDispenserState : SyncAggregateEntity
{
    public Guid DeviceId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid IngredientId { get; set; }

    public Guid? KioskIngredientInventoryId { get; set; }

    public string ContainerCode { get; set; } = null!;

    public IngredientLevelStatus CurrentLevelStatus { get; set; } = IngredientLevelStatus.Unknown;

    public decimal? EstimatedQuantity { get; set; }

    public decimal? CapacityQuantity { get; set; }

    public string Unit { get; set; } = "gram";

    public int LevelToQuantityProfileSchemaVersion { get; set; } = 1;

    public string? LevelToQuantityProfileJson { get; set; }

    public InventoryTrackingMode TrackingMode { get; private set; } = InventoryTrackingMode.ManualEstimate;

    public DateTimeOffset LastMeasuredAt { get; set; }

    public DateTimeOffset? LastSensorObservedAt { get; private set; }

    public decimal? LastObservedEstimatedQuantity { get; private set; }

    public bool SensorRebaselineRequired { get; private set; }

    public DateTimeOffset? SensorRebaselineRequestedAt { get; private set; }

    public Guid? SensorRebaselineRefillTaskId { get; private set; }

    public DateTimeOffset? LastRefilledAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public string? SensorPayloadJson { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Device Device { get; set; } = null!;

    public virtual Kiosk? Kiosk { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual KioskIngredientInventory? KioskIngredientInventory { get; set; }

    public void ConfigureContainer(decimal? capacityQuantity, string unit, string? levelToQuantityProfileJson = null)
    {
        EnsureActive();
        if (capacityQuantity.HasValue && capacityQuantity.Value <= 0)
        {
            throw new DomainRuleException("Dispenser capacity must be greater than zero when provided.");
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new DomainRuleException("Dispenser unit is required.");
        }

        if (capacityQuantity.HasValue && EstimatedQuantity.HasValue && EstimatedQuantity.Value > capacityQuantity.Value)
        {
            throw new DomainRuleException("Dispenser capacity cannot be lower than its current estimated quantity.");
        }

        CapacityQuantity = capacityQuantity;
        Unit = unit.Trim();
        LevelToQuantityProfileJson = levelToQuantityProfileJson;
    }

    public void RecordSensorLevel(
        IngredientLevelStatus levelStatus,
        DateTimeOffset measuredAt,
        string? sensorPayloadJson = null,
        decimal? estimatedQuantity = null)
    {
        EnsureActive();
        if (estimatedQuantity.HasValue && estimatedQuantity.Value < 0)
        {
            throw new DomainRuleException("Estimated quantity cannot be negative.");
        }

        if (estimatedQuantity.HasValue && CapacityQuantity.HasValue && estimatedQuantity.Value > CapacityQuantity.Value)
        {
            throw new DomainRuleException("Estimated quantity exceeds dispenser capacity.");
        }

        CurrentLevelStatus = levelStatus;
        EstimatedQuantity = estimatedQuantity;
        LastMeasuredAt = measuredAt;
        LastSensorObservedAt = measuredAt;
        SensorPayloadJson = sensorPayloadJson;
        LastObservedEstimatedQuantity = estimatedQuantity;
    }

    public void RequireSensorRebaseline(Guid refillTaskId, DateTimeOffset requestedAt)
    {
        EnsureActive();
        if (refillTaskId == Guid.Empty) throw new DomainRuleException("Refill task is required for sensor rebaseline.");
        SensorRebaselineRequired = true;
        SensorRebaselineRequestedAt = requestedAt;
        SensorRebaselineRefillTaskId = refillTaskId;
    }

    public bool ConsumeSensorRebaseline()
    {
        if (!SensorRebaselineRequired) return false;
        SensorRebaselineRequired = false;
        SensorRebaselineRequestedAt = null;
        SensorRebaselineRefillTaskId = null;
        return true;
    }

    public void ChangeTrackingMode(InventoryTrackingMode trackingMode)
    {
        if (!Enum.IsDefined(trackingMode))
        {
            throw new DomainRuleException("Inventory tracking mode is invalid.");
        }

        TrackingMode = trackingMode;
    }

    public StockMovement Refill(
        decimal quantity,
        DateTimeOffset occurredAt,
        string? reasonCode = "REFILL",
        Guid? sourceEventId = null,
        IngredientLevelStatus? reportedLevelAfter = IngredientLevelStatus.Full)
    {
        EnsureActive();
        if (quantity <= 0)
        {
            throw new DomainRuleException("Refill quantity must be greater than zero.");
        }

        var previousEstimate = EstimatedQuantity;
        if (EstimatedQuantity.HasValue)
        {
            var newEstimatedQuantity = EstimatedQuantity.Value + quantity;

            if (CapacityQuantity.HasValue && newEstimatedQuantity > CapacityQuantity.Value)
            {
                throw new DomainRuleException("Refill quantity exceeds dispenser capacity estimate.");
            }

            EstimatedQuantity = newEstimatedQuantity;
        }

        if (reportedLevelAfter.HasValue)
        {
            CurrentLevelStatus = reportedLevelAfter.Value;
        }

        LastRefilledAt = occurredAt;
        LastMeasuredAt = occurredAt;

        return AddMovement("REFILL", quantity, previousEstimate, occurredAt, reasonCode, sourceEventId: sourceEventId);
    }

    public StockMovement Consume(
        decimal quantity,
        DateTimeOffset occurredAt,
        string? referenceType = null,
        Guid? referenceId = null,
        Guid? sourceEventId = null,
        IngredientLevelStatus? reportedLevelAfter = null)
    {
        return ConsumeWithEvidence(
            quantity,
            occurredAt,
            reportedBalanceAfter: null,
            referenceType,
            referenceId,
            sourceEventId,
            reportedLevelAfter);
    }

    public StockMovement ConsumeWithEvidence(
        decimal quantity,
        DateTimeOffset occurredAt,
        decimal? reportedBalanceAfter,
        string? referenceType = null,
        Guid? referenceId = null,
        Guid? sourceEventId = null,
        IngredientLevelStatus? reportedLevelAfter = null)
    {
        EnsureActive();
        if (quantity <= 0)
        {
            throw new DomainRuleException("Consumed quantity must be greater than zero.");
        }

        var previousEstimate = EstimatedQuantity;
        if (previousEstimate.HasValue)
        {
            if (previousEstimate.Value < quantity)
            {
                throw new DomainRuleException("Not enough estimated ingredient quantity in dispenser.");
            }

            var expectedBalanceAfter = previousEstimate.Value - quantity;
            if (reportedBalanceAfter.HasValue && reportedBalanceAfter.Value != expectedBalanceAfter)
            {
                throw new DomainRuleException(
                    "Reported stock balance does not match the dispenser estimate after consumption.");
            }

            EstimatedQuantity = expectedBalanceAfter;
        }
        else if (reportedBalanceAfter.HasValue)
        {
            if (reportedBalanceAfter.Value < 0)
            {
                throw new DomainRuleException("Reported stock balance cannot be negative.");
            }

            if (CapacityQuantity.HasValue && reportedBalanceAfter.Value > CapacityQuantity.Value)
            {
                throw new DomainRuleException("Reported stock balance exceeds dispenser capacity.");
            }

            EstimatedQuantity = reportedBalanceAfter.Value;
        }

        if (reportedLevelAfter.HasValue)
        {
            CurrentLevelStatus = reportedLevelAfter.Value;
        }

        LastMeasuredAt = occurredAt;

        return AddMovement("CONSUME", -quantity, previousEstimate, occurredAt, null, referenceType, referenceId, sourceEventId);
    }

    public StockMovement AdjustEstimate(
        decimal estimatedQuantity,
        DateTimeOffset occurredAt,
        string reasonCode,
        Guid? sourceEventId = null,
        IngredientLevelStatus? reportedLevelAfter = null)
    {
        EnsureActive();
        if (estimatedQuantity < 0)
        {
            throw new DomainRuleException("Estimated quantity cannot be negative.");
        }

        if (CapacityQuantity.HasValue && estimatedQuantity > CapacityQuantity.Value)
        {
            throw new DomainRuleException("Estimated quantity exceeds dispenser capacity.");
        }

        var previousEstimate = EstimatedQuantity;
        var delta = previousEstimate.HasValue ? estimatedQuantity - previousEstimate.Value : estimatedQuantity;

        EstimatedQuantity = estimatedQuantity;
        LastMeasuredAt = occurredAt;

        if (reportedLevelAfter.HasValue)
        {
            CurrentLevelStatus = reportedLevelAfter.Value;
        }

        return AddMovement("ADJUST_ESTIMATE", delta, previousEstimate, occurredAt, reasonCode, sourceEventId: sourceEventId, isEstimated: true);
    }

    public bool IsLow()
    {
        return CurrentLevelStatus == IngredientLevelStatus.Low;
    }

    public bool IsFull()
    {
        return CurrentLevelStatus == IngredientLevelStatus.Full;
    }

    public void Retire(Guid? actorId, DateTimeOffset now)
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    public void Reactivate(Guid? actorId, DateTimeOffset now)
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainRuleException("Retired dispenser state cannot accept inventory updates.");
        }

    }

    private StockMovement AddMovement(
        string movementType,
        decimal quantity,
        decimal? balanceBefore,
        DateTimeOffset occurredAt,
        string? reasonCode = null,
        string? referenceType = null,
        Guid? referenceId = null,
        Guid? sourceEventId = null,
        bool isEstimated = false)
    {
        var movement = StockMovement.Create(
            Id,
            null,
            null,
            KioskId,
            DeviceId,
            IngredientId,
            movementType,
            quantity,
            balanceBefore,
            EstimatedQuantity,
            Unit,
            occurredAt,
            reasonCode,
            referenceType,
            referenceId,
            sourceEventId,
            isEstimated);

        return movement;
    }
}
