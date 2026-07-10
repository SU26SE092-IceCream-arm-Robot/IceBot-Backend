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
namespace Infrastructure.Data.Configurations.Sync;

internal sealed class SyncEventInboxConfiguration : IEntityTypeConfiguration<SyncEventInbox>
{
    public void Configure(EntityTypeBuilder<SyncEventInbox> entity)
    {
        entity.ToTable("SyncEventInbox");
        entity.HasIndex(x => new { x.SourceNodeId, x.EventId }).IsUnique();
        entity.HasIndex(x => new { x.SourceNodeId, x.EventType, x.OccurredAt });
        entity.HasIndex(x => new { x.SourceNodeId, x.SequenceNumber })
            .IsUnique()
            .HasFilter("\"SequenceNumber\" IS NOT NULL");
        entity.HasIndex(x => new { x.Status, x.NextRetryAt, x.LockedUntil });
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class ProductionEventCheckpointConfiguration : IEntityTypeConfiguration<ProductionEventCheckpoint>
{
    public void Configure(EntityTypeBuilder<ProductionEventCheckpoint> entity)
    {
        entity.ToTable("ProductionEventCheckpoints");
        entity.HasIndex(x => x.SourceExecutorId).IsUnique();
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.KioskExecutionEndpoint)
            .WithMany()
            .HasForeignKey(x => new { x.KioskExecutionEndpointId, x.KioskId })
            .HasPrincipalKey(endpoint => new { endpoint.Id, endpoint.KioskId })
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class EdgeStateSummaryConfiguration : IEntityTypeConfiguration<EdgeStateSummary>
{
    public void Configure(EntityTypeBuilder<EdgeStateSummary> entity)
    {
        entity.ToTable("EdgeStateSummaries");
        entity.HasIndex(x => new { x.SourceExecutorId, x.SummaryKind }).IsUnique();
        entity.Property(x => x.SummaryKind).HasMaxLength(100);
        entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.KioskExecutionEndpoint)
            .WithMany()
            .HasForeignKey(x => new { x.KioskExecutionEndpointId, x.KioskId })
            .HasPrincipalKey(endpoint => new { endpoint.Id, endpoint.KioskId })
            .OnDelete(DeleteBehavior.Restrict);

    }
}
