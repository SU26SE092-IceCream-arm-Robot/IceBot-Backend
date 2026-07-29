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
        entity.HasOne(x => x.Device).WithMany(x => x.IngredientDispenserStates).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);

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

    }
}
