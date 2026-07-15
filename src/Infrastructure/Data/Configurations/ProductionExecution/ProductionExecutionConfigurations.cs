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
namespace Infrastructure.Data.Configurations.ProductionExecution;

internal sealed class OrderExecutionRecordConfiguration : IEntityTypeConfiguration<OrderExecutionRecord>
{
    public void Configure(EntityTypeBuilder<OrderExecutionRecord> entity)
    {
        entity.ToTable("OrderExecutionRecords");
        entity.HasIndex(x => x.SourceCommandId).IsUnique();
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.SourceExecutorId, x.LastAppliedSourceEventId }).IsUnique();
        entity.HasIndex(x => new { x.OrderId, x.CloudReceivedAt });
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Status, x.LastExecutorReportedAt });
        entity.HasOne(x => x.KioskExecutionEndpoint)
            .WithMany()
            .HasForeignKey(x => x.KioskExecutionEndpointId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.SourceCommand)
            .WithMany()
            .HasForeignKey(x => new { x.SourceCommandId, x.KioskExecutionEndpointId })
            .HasPrincipalKey(x => new { x.Id, x.TargetExecutionEndpointId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.SourceConfigurationRelease)
            .WithMany()
            .HasForeignKey(x => x.SourceConfigurationReleaseId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class ProductionExecutionRecordConfiguration : IEntityTypeConfiguration<ProductionExecutionRecord>
{
    public void Configure(EntityTypeBuilder<ProductionExecutionRecord> entity)
    {
        entity.ToTable("ProductionExecutionRecords");
        entity.HasIndex(x => new { x.SourceCommandId, x.SourceProductionJobId }).IsUnique();
        entity.HasIndex(x => new { x.SourceCommandId, x.OrderItemId, x.ProductionUnitNo }).IsUnique();
        entity.HasOne<OrderItem>().WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.SourceExecutorId, x.LastAppliedSourceEventId }).IsUnique();
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Status, x.LastExecutorReportedAt });
        entity.HasOne(x => x.KioskExecutionEndpoint)
            .WithMany()
            .HasForeignKey(x => x.KioskExecutionEndpointId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.SourceCommand)
            .WithMany()
            .HasForeignKey(x => new { x.SourceCommandId, x.KioskExecutionEndpointId })
            .HasPrincipalKey(x => new { x.Id, x.TargetExecutionEndpointId })
            .OnDelete(DeleteBehavior.Restrict);

    }
}
