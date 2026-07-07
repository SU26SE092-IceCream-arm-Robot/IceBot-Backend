using Domain.Catalog.Entities;
using Domain.Common;
using Domain.Devices.Entities;
using Domain.Inventory.Enums;
using Domain.Tenants.Entities;

namespace Domain.Inventory.Entities;

public partial class IngredientDispenserState : SyncAggregateEntity
{
    public Guid DeviceId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid IngredientId { get; set; }

    public string ContainerCode { get; set; } = null!;

    public IngredientLevelStatus CurrentLevelStatus { get; set; } = IngredientLevelStatus.Unknown;

    public decimal? EstimatedQuantity { get; set; }

    public decimal? CapacityQuantity { get; set; }

    public string Unit { get; set; } = "gram";

    public int LevelToQuantityProfileSchemaVersion { get; set; } = 1;

    public string? LevelToQuantityProfileJson { get; set; }

    public DateTimeOffset LastMeasuredAt { get; set; }

    public DateTimeOffset? LastRefilledAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public string? SensorPayloadJson { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Device Device { get; set; } = null!;

    public virtual Kiosk? Kiosk { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

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
        SensorPayloadJson = sensorPayloadJson;
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
        EnsureActive();
        if (quantity <= 0)
        {
            throw new DomainRuleException("Consumed quantity must be greater than zero.");
        }

        var previousEstimate = EstimatedQuantity;
        if (EstimatedQuantity.HasValue)
        {
            if (EstimatedQuantity.Value < quantity)
            {
                throw new DomainRuleException("Not enough estimated ingredient quantity in dispenser.");
            }

            EstimatedQuantity -= quantity;
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
