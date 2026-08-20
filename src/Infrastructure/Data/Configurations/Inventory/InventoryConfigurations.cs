using Domain.Catalog.Entities;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.ExecutionEndpoints.Projections;
using Domain.Devices.Telemetry;
using Domain.Identity.Entities;
using Domain.Identity.ValueObjects;
using Domain.Inventory.Entities;
using Domain.Operations.Entities;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionExecution.Projections;
using Domain.RobotConfiguration.Artifacts;
using Domain.SalesCatalog.Entities;
using Domain.Sync.DeadLetters;
using Domain.Sync.Entities;
using Domain.Sync.Ingestion;
using Domain.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Infrastructure.Data.Configurations.Inventory;

internal sealed class IngredientDispenserStateConfiguration : IEntityTypeConfiguration<IngredientDispenserState>
{
    public void Configure(EntityTypeBuilder<IngredientDispenserState> entity)
    {
        entity.ToTable("IngredientDispenserStates");
        entity.HasIndex(x => new { x.DeviceId, x.ContainerCode }).IsUnique()
            .HasFilter("\"IsActive\" = TRUE AND \"DeletedAt\" IS NULL");
        entity.Property(x => x.IsActive).HasDefaultValue(true);
        entity.Property(x => x.TrackingMode).HasDefaultValue(Domain.Inventory.Enums.InventoryTrackingMode.ManualEstimate);
        entity.HasOne(x => x.Device).WithMany(x => x.IngredientDispenserStates).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.KioskIngredientInventory)
            .WithMany()
            .HasForeignKey(x => x.KioskIngredientInventoryId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class InventoryTopologyRebindRecordConfiguration : IEntityTypeConfiguration<InventoryTopologyRebindRecord>
{
    public void Configure(EntityTypeBuilder<InventoryTopologyRebindRecord> entity)
    {
        entity.ToTable("InventoryTopologyRebindRecords");
        entity.HasIndex(x => x.SourceDispenserStateId).IsUnique();
        entity.HasIndex(x => x.ReplacementDispenserStateId).IsUnique();
        entity.HasIndex(x => new { x.KioskId, x.CreatedAt });
        entity.Property(x => x.SourceContainerCode).HasMaxLength(50);
        entity.Property(x => x.ReplacementContainerCode).HasMaxLength(50);
        entity.Property(x => x.SourceUnit).HasMaxLength(30);
        entity.Property(x => x.ReplacementUnit).HasMaxLength(30);
        entity.Property(x => x.Reason).HasMaxLength(500);
        entity.HasOne<IngredientDispenserState>()
            .WithMany()
            .HasForeignKey(x => x.SourceDispenserStateId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<IngredientDispenserState>()
            .WithMany()
            .HasForeignKey(x => x.ReplacementDispenserStateId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class InventoryTopologyChangeRecordConfiguration : IEntityTypeConfiguration<InventoryTopologyChangeRecord>
{
    public void Configure(EntityTypeBuilder<InventoryTopologyChangeRecord> entity)
    {
        entity.ToTable("InventoryTopologyChangeRecords");
        entity.HasIndex(x => new { x.DispenserStateId, x.CreatedAt });
        entity.HasIndex(x => new { x.KioskId, x.CreatedAt });
        entity.Property(x => x.ContainerCode).HasMaxLength(50);
        entity.Property(x => x.BeforeUnit).HasMaxLength(30);
        entity.Property(x => x.AfterUnit).HasMaxLength(30);
        entity.Property(x => x.Reason).HasMaxLength(500);

    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> entity)
    {
        entity.ToTable("StockMovements");
        entity.HasIndex(x => x.SourceEventId).IsUnique().HasFilter("\"SourceEventId\" IS NOT NULL");
        entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.OccurredAt });
        entity.HasOne(x => x.CreatedByAccount).WithMany().HasForeignKey(x => x.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.IngredientDispenserState).WithMany().HasForeignKey(x => x.IngredientDispenserStateId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.KioskIngredientInventory).WithMany().HasForeignKey(x => x.KioskIngredientInventoryId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => x.KioskIngredientInventoryId);

    }
}

internal sealed class KioskIngredientInventoryConfiguration : IEntityTypeConfiguration<KioskIngredientInventory>
{
    public void Configure(EntityTypeBuilder<KioskIngredientInventory> entity)
    {
        entity.ToTable("KioskIngredientInventories");
        entity.HasIndex(x => new { x.KioskId, x.IngredientId, x.Unit }).IsUnique();
        entity.Property(x => x.Unit).HasMaxLength(30);
        entity.Property(x => x.TrackingMode).HasDefaultValue(Domain.Inventory.Enums.InventoryTrackingMode.ManualEstimate);
        entity.Property(x => x.IsActive).HasDefaultValue(true);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InventoryRefillTaskConfiguration : IEntityTypeConfiguration<InventoryRefillTask>
{
    public void Configure(EntityTypeBuilder<InventoryRefillTask> entity)
    {
        entity.ToTable("InventoryRefillTasks");
        entity.Property(x => x.Unit).HasMaxLength(30);
        entity.Property(x => x.ReasonCode).HasMaxLength(100);
        entity.Property(x => x.Notes).HasMaxLength(1_000);
        entity.Property(x => x.ExternalLotReference).HasMaxLength(200);
        entity.Property(x => x.RequestIdempotencyKey).HasMaxLength(200);
        entity.Property(x => x.RequestFingerprint).HasMaxLength(128);
        entity.HasIndex(x => new { x.KioskId, x.RequestIdempotencyKey }).IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
        entity.HasIndex(x => x.KioskIngredientInventoryId).IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL AND \"Status\" IN (1, 2)");
        entity.HasIndex(x => new { x.KioskId, x.Status, x.RequestedAt });
        entity.HasOne<KioskIngredientInventory>().WithMany().HasForeignKey(x => x.KioskIngredientInventoryId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<IngredientDispenserState>().WithMany().HasForeignKey(x => x.IngredientDispenserStateId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Alert>().WithMany().HasForeignKey(x => x.SourceAlertId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Account>().WithMany().HasForeignKey(x => x.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Account>().WithMany().HasForeignKey(x => x.StartedByAccountId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Account>().WithMany().HasForeignKey(x => x.CompletedByAccountId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Account>().WithMany().HasForeignKey(x => x.CancelledByAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InventoryRefillTaskTransitionConfiguration : IEntityTypeConfiguration<InventoryRefillTaskTransition>
{
    public void Configure(EntityTypeBuilder<InventoryRefillTaskTransition> entity)
    {
        entity.ToTable("InventoryRefillTaskTransitions");
        entity.Property(x => x.ActorRoleCode).HasMaxLength(50);
        entity.Property(x => x.Reason).HasMaxLength(1_000);
        entity.Property(x => x.RequestIdempotencyKey).HasMaxLength(200);
        entity.Property(x => x.RequestFingerprint).HasMaxLength(128);
        entity.HasIndex(x => new { x.InventoryRefillTaskId, x.RequestIdempotencyKey }).IsUnique();
        entity.HasOne<InventoryRefillTask>().WithMany().HasForeignKey(x => x.InventoryRefillTaskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Account>().WithMany().HasForeignKey(x => x.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InventoryReconciliationCaseConfiguration : IEntityTypeConfiguration<InventoryReconciliationCase>
{
    public void Configure(EntityTypeBuilder<InventoryReconciliationCase> entity)
    {
        entity.ToTable("InventoryReconciliationCases");
        entity.HasIndex(x => new { x.SourceEventId, x.IngredientId, x.Unit, x.ReasonCode }).IsUnique();
        entity.Property(x => x.Unit).HasMaxLength(30);
        entity.Property(x => x.ReasonCode).HasMaxLength(100);
        entity.Property(x => x.ResolutionNote).HasMaxLength(1_000);
        entity.HasOne<KioskIngredientInventory>().WithMany().HasForeignKey(x => x.KioskIngredientInventoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class InventorySensorObservationConfiguration : IEntityTypeConfiguration<InventorySensorObservation>
{
    public void Configure(EntityTypeBuilder<InventorySensorObservation> entity)
    {
        entity.ToTable("InventorySensorObservations");
        entity.HasIndex(x => new { x.SourceExecutorId, x.SourceEventId }).IsUnique();
        entity.HasIndex(x => new { x.IngredientDispenserStateId, x.CloudReceivedAt });
        entity.HasIndex(x => new { x.SourceExecutorId, x.IngredientDispenserStateId, x.ObservationSequence });
        entity.HasOne<IngredientDispenserState>()
            .WithMany()
            .HasForeignKey(x => x.IngredientDispenserStateId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<KioskExecutionEndpoint>()
            .WithMany()
            .HasForeignKey(x => x.KioskExecutionEndpointId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.Property(x => x.SensorPayloadJson).HasMaxLength(16_384);
    }
}
