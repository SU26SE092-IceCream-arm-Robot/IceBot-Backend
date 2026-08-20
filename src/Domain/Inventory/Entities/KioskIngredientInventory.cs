using Domain.Catalog.Entities;
using Domain.Common;
using Domain.Inventory.Enums;
using Domain.Tenants.Entities;

namespace Domain.Inventory.Entities;

/// <summary>Current Cloud inventory authority for one kiosk ingredient and unit.</summary>
public sealed class KioskIngredientInventory : SyncAggregateEntity
{
    public Guid OrganizationId { get; set; }
    public Guid StoreId { get; set; }
    public Guid KioskId { get; set; }
    public Guid IngredientId { get; set; }
    public string Unit { get; private set; } = "gram";
    public decimal? EstimatedQuantity { get; private set; }
    public decimal? LowStockThreshold { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public InventoryTrackingMode TrackingMode { get; private set; } = InventoryTrackingMode.ManualEstimate;
    public DateTimeOffset LastMeasuredAt { get; private set; }
    public DateTimeOffset? LastSensorReconciledAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Kiosk Kiosk { get; set; } = null!;
    public Ingredient Ingredient { get; set; } = null!;

    public void Configure(string unit, decimal? estimatedQuantity, decimal? lowStockThreshold, DateTimeOffset? expiresAt, InventoryTrackingMode trackingMode, DateTimeOffset now)
    {
        Unit = NormalizeUnit(unit);
        EnsureQuantity(estimatedQuantity);
        EnsureThreshold(lowStockThreshold);
        EnsureTrackingMode(trackingMode);
        EstimatedQuantity = estimatedQuantity;
        LowStockThreshold = lowStockThreshold;
        ExpiresAt = expiresAt;
        TrackingMode = trackingMode;
        LastMeasuredAt = now;
    }

    public void AdjustEstimate(decimal estimatedQuantity, DateTimeOffset now)
    {
        EnsureActive();
        EnsureQuantity(estimatedQuantity);
        EstimatedQuantity = estimatedQuantity;
        LastMeasuredAt = now;
    }

    public void Refill(decimal quantity, DateTimeOffset now)
    {
        EnsureActive();
        if (quantity <= 0) throw new DomainRuleException("Refill quantity must be greater than zero.");
        if (EstimatedQuantity.HasValue) EstimatedQuantity += quantity;
        LastMeasuredAt = now;
    }

    public decimal ConsumeAvailable(decimal quantity, DateTimeOffset now)
    {
        EnsureActive();
        if (quantity <= 0) throw new DomainRuleException("Consumed quantity must be greater than zero.");
        if (!EstimatedQuantity.HasValue) return 0;
        var applied = Math.Min(EstimatedQuantity.Value, quantity);
        EstimatedQuantity -= applied;
        LastMeasuredAt = now;
        return applied;
    }

    public void ReconcileSensorDelta(decimal newContribution, decimal? previousContribution, DateTimeOffset now)
    {
        EnsureActive();
        EnsureQuantity(newContribution);
        var delta = newContribution - (previousContribution ?? newContribution);
        if (EstimatedQuantity.HasValue)
            EstimatedQuantity = Math.Max(0, EstimatedQuantity.Value + delta);
        LastMeasuredAt = now;
        LastSensorReconciledAt = now;
    }

    public void ChangeTrackingMode(InventoryTrackingMode trackingMode, DateTimeOffset now)
    {
        EnsureTrackingMode(trackingMode);
        TrackingMode = trackingMode;
        LastMeasuredAt = now;
    }

    public void UpdateMetadata(decimal? lowStockThreshold, DateTimeOffset? expiresAt, InventoryTrackingMode trackingMode, DateTimeOffset now)
    {
        EnsureActive();
        EnsureTrackingMode(trackingMode);
        EnsureThreshold(lowStockThreshold);
        LowStockThreshold = lowStockThreshold;
        ExpiresAt = expiresAt;
        TrackingMode = trackingMode;
        LastMeasuredAt = now;
    }

    private static string NormalizeUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) throw new DomainRuleException("Inventory unit is required.");
        return unit.Trim().ToLowerInvariant();
    }

    private static void EnsureQuantity(decimal? quantity)
    {
        if (quantity.HasValue && quantity.Value < 0) throw new DomainRuleException("Estimated quantity cannot be negative.");
    }

    private static void EnsureThreshold(decimal? threshold)
    {
        if (threshold.HasValue && threshold.Value < 0) throw new DomainRuleException("Low stock threshold cannot be negative.");
    }

    private static void EnsureTrackingMode(InventoryTrackingMode trackingMode)
    {
        if (!Enum.IsDefined(trackingMode)) throw new DomainRuleException("Inventory tracking mode is invalid.");
    }

    private void EnsureActive()
    {
        if (!IsActive) throw new DomainRuleException("Retired kiosk inventory cannot be updated.");
    }
}
