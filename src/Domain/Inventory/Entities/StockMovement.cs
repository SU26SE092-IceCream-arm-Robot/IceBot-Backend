using Domain.Catalog.Entities;
using Domain.Common;
using Domain.Devices.Entities;
using Domain.Identity.Entities;
using Domain.Tenants.Entities;

namespace Domain.Inventory.Entities;

public partial class StockMovement : AppendOnlySyncEntity, IStoreScoped
{
    public Guid IngredientDispenserStateId { get; set; }

    public Guid? OrganizationId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? IngredientId { get; set; }

    public Guid? SourceEventId { get; set; }

    public Guid? CorrelationId { get; set; }

    public Guid? CausationId { get; set; }

    public string MovementType { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal? BalanceBefore { get; set; }

    public decimal? BalanceAfter { get; set; }

    public bool IsEstimated { get; set; }

    public string Unit { get; set; } = "gram";

    public string? ReasonCode { get; set; }

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public virtual Account? CreatedByAccount { get; set; }

    public virtual Organization? Organization { get; set; }

    public virtual Store? Store { get; set; }

    public virtual Kiosk? Kiosk { get; set; }

    public virtual Device? Device { get; set; }

    public virtual Ingredient? Ingredient { get; set; }

    public virtual IngredientDispenserState IngredientDispenserState { get; set; } = null!;

    public static StockMovement Create(
        Guid ingredientDispenserStateId,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? deviceId,
        Guid? ingredientId,
        string movementType,
        decimal quantity,
        decimal? balanceBefore,
        decimal? balanceAfter,
        string unit,
        DateTimeOffset occurredAt,
        string? reasonCode = null,
        string? referenceType = null,
        Guid? referenceId = null,
        Guid? sourceEventId = null,
        bool isEstimated = false)
    {
        if (ingredientDispenserStateId == Guid.Empty)
        {
            throw new DomainRuleException("Ingredient dispenser state is required for stock movement.");
        }

        if (string.IsNullOrWhiteSpace(movementType))
        {
            throw new DomainRuleException("Stock movement type is required.");
        }

        if (quantity == 0)
        {
            throw new DomainRuleException("Stock movement quantity cannot be zero.");
        }

        return new StockMovement
        {
            IngredientDispenserStateId = ingredientDispenserStateId,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            DeviceId = deviceId,
            IngredientId = ingredientId,
            MovementType = movementType.Trim(),
            Quantity = quantity,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            IsEstimated = isEstimated,
            Unit = string.IsNullOrWhiteSpace(unit) ? "unit" : unit.Trim(),
            OccurredAt = occurredAt,
            ReasonCode = reasonCode,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            SourceEventId = sourceEventId
        };
    }
}
