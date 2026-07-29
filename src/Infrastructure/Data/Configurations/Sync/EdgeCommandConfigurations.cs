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

internal sealed class EdgeCommandConfiguration : IEntityTypeConfiguration<EdgeCommand>
{
    public void Configure(EntityTypeBuilder<EdgeCommand> entity)
    {
        entity.ToTable("EdgeCommands");
        entity.HasAlternateKey(x => new { x.Id, x.TargetExecutionEndpointId });
        entity.HasIndex(x => new { x.TargetExecutionEndpointId, x.Status, x.CreatedAt });
        entity.HasIndex(x => x.DeploymentId).IsUnique().HasFilter("\"DeploymentId\" IS NOT NULL");
        entity.HasIndex(x => new { x.OrderId, x.DispatchAttemptNo }).IsUnique().HasFilter("\"OrderId\" IS NOT NULL AND \"DispatchAttemptNo\" IS NOT NULL");
        entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
        entity.Navigation(x => x.DeliveryAttempts).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.HasOne(x => x.TargetExecutionEndpoint)
            .WithMany()
            .HasForeignKey(x => new { x.TargetExecutionEndpointId, x.KioskId })
            .HasPrincipalKey(endpoint => new { endpoint.Id, endpoint.KioskId })
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class EdgeCommandDeliveryAttemptConfiguration : IEntityTypeConfiguration<EdgeCommandDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<EdgeCommandDeliveryAttempt> entity)
    {
        entity.ToTable("EdgeCommandDeliveryAttempts");
        entity.HasIndex(x => new { x.EdgeCommandId, x.DeliveryAttemptNo }).IsUnique();
        entity.HasOne(x => x.EdgeCommand).WithMany(x => x.DeliveryAttempts).HasForeignKey(x => x.EdgeCommandId).OnDelete(DeleteBehavior.Restrict);

    }
}
