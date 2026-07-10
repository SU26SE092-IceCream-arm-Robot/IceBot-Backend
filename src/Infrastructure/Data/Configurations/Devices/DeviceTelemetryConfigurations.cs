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
namespace Infrastructure.Data.Configurations.Devices;

internal sealed class DeviceEventConfiguration : IEntityTypeConfiguration<DeviceEvent>
{
    public void Configure(EntityTypeBuilder<DeviceEvent> entity)
    {
        entity.ToTable("DeviceEvents");
        entity.HasIndex(x => x.EventId).IsUnique();
        entity.HasIndex(x => new { x.DeviceId, x.OccurredAt });
        entity.HasIndex(x => new { x.KioskId, x.OccurredAt });
        entity.HasOne(x => x.Device).WithMany()
            .HasForeignKey(x => new { x.DeviceId, x.KioskId })
            .HasPrincipalKey(x => new { x.Id, x.KioskId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class KioskHeartbeatConfiguration : IEntityTypeConfiguration<KioskHeartbeat>
{
    public void Configure(EntityTypeBuilder<KioskHeartbeat> entity)
    {
        entity.ToTable("KioskHeartbeats");
        entity.HasIndex(x => new { x.KioskId, x.NodeId, x.HeartbeatSequence }).IsUnique().HasFilter("\"HeartbeatSequence\" IS NOT NULL");
        entity.HasIndex(x => new { x.KioskId, x.ReportedAt });
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);

    }
}
