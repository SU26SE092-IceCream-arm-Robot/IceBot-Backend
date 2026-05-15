using Domain.Common;

namespace Domain.Entities;

public partial class IngredientDispenserState : RobotRuntimeAggregateEntity
{
    public Guid DeviceId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid IngredientId { get; set; }

    public string ContainerCode { get; set; } = null!;

    public decimal CurrentQuantity { get; set; }

    public decimal CapacityQuantity { get; set; }

    public string Unit { get; set; } = "gram";

    public decimal? LowThreshold { get; set; }

    public decimal? CriticalThreshold { get; set; }

    public string? CurrentLevelStatus { get; set; }

    public DateTimeOffset LastMeasuredAt { get; set; }

    public DateTimeOffset? LastRefilledAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public string? SensorPayloadJson { get; set; }

    public virtual Device Device { get; set; } = null!;

    public virtual Kiosk? Kiosk { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public void ConfigureCapacity(decimal capacityQuantity, string unit, decimal? lowThreshold = null, decimal? criticalThreshold = null)
    {
        if (capacityQuantity <= 0)
        {
            throw new DomainRuleException("Dispenser capacity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new DomainRuleException("Dispenser unit is required.");
        }

        if (lowThreshold.HasValue && lowThreshold.Value < 0)
        {
            throw new DomainRuleException("Low threshold cannot be negative.");
        }

        if (criticalThreshold.HasValue && criticalThreshold.Value < 0)
        {
            throw new DomainRuleException("Critical threshold cannot be negative.");
        }

        if (criticalThreshold.HasValue && lowThreshold.HasValue && criticalThreshold.Value > lowThreshold.Value)
        {
            throw new DomainRuleException("Critical threshold cannot be greater than low threshold.");
        }

        CapacityQuantity = capacityQuantity;
        Unit = unit.Trim();
        LowThreshold = lowThreshold;
        CriticalThreshold = criticalThreshold;
    }

    public StockMovement Refill(decimal quantity, DateTimeOffset occurredAt, string? reasonCode = "REFILL", Guid? sourceEventId = null)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("Refill quantity must be greater than zero.");
        }

        var newQuantity = CurrentQuantity + quantity;

        if (CapacityQuantity > 0 && newQuantity > CapacityQuantity)
        {
            throw new DomainRuleException("Refill quantity exceeds dispenser capacity.");
        }

        CurrentQuantity = newQuantity;
        LastRefilledAt = occurredAt;
        LastMeasuredAt = occurredAt;
        UpdateLevelStatus();

        return AddMovement("REFILL", quantity, occurredAt, reasonCode, sourceEventId: sourceEventId);
    }

    public StockMovement Consume(decimal quantity, DateTimeOffset occurredAt, string? referenceType = null, Guid? referenceId = null, Guid? sourceEventId = null)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleException("Consumed quantity must be greater than zero.");
        }

        if (CurrentQuantity < quantity)
        {
            throw new DomainRuleException("Not enough ingredient quantity in dispenser.");
        }

        CurrentQuantity -= quantity;
        LastMeasuredAt = occurredAt;
        UpdateLevelStatus();

        return AddMovement("CONSUME", -quantity, occurredAt, null, referenceType, referenceId, sourceEventId);
    }

    public StockMovement Adjust(decimal newQuantity, DateTimeOffset occurredAt, string reasonCode, Guid? sourceEventId = null)
    {
        if (newQuantity < 0)
        {
            throw new DomainRuleException("Adjusted quantity cannot be negative.");
        }

        if (CapacityQuantity > 0 && newQuantity > CapacityQuantity)
        {
            throw new DomainRuleException("Adjusted quantity exceeds dispenser capacity.");
        }

        var delta = newQuantity - CurrentQuantity;
        CurrentQuantity = newQuantity;
        LastMeasuredAt = occurredAt;
        UpdateLevelStatus();

        return AddMovement("ADJUST", delta, occurredAt, reasonCode, sourceEventId: sourceEventId);
    }

    public bool IsLow()
    {
        return LowThreshold.HasValue && CurrentQuantity <= LowThreshold.Value;
    }

    public bool IsCritical()
    {
        return CriticalThreshold.HasValue && CurrentQuantity <= CriticalThreshold.Value;
    }

    private StockMovement AddMovement(
        string movementType,
        decimal quantity,
        DateTimeOffset occurredAt,
        string? reasonCode = null,
        string? referenceType = null,
        Guid? referenceId = null,
        Guid? sourceEventId = null)
    {
        var movement = StockMovement.Create(
            Id,
            KioskId,
            DeviceId,
            IngredientId,
            movementType,
            quantity,
            CurrentQuantity,
            Unit,
            occurredAt,
            reasonCode,
            referenceType,
            referenceId,
            sourceEventId);

        StockMovements.Add(movement);
        return movement;
    }

    private void UpdateLevelStatus()
    {
        CurrentLevelStatus = IsCritical()
            ? "Critical"
            : IsLow()
                ? "Low"
                : "Normal";
    }
}
