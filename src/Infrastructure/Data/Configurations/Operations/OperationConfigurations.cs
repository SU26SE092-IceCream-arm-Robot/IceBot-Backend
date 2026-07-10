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
namespace Infrastructure.Data.Configurations.Operations;

internal sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> entity)
    {
        entity.ToTable("Alerts");
        entity.HasIndex(x => new { x.KioskId, x.Status, x.RaisedAt });
        entity.HasIndex(x => new { x.KioskId, x.DeviceId, x.CorrelationKey, x.Status, x.LastOccurredAt });
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Device).WithMany()
            .HasForeignKey(x => new { x.DeviceId, x.KioskId })
            .HasPrincipalKey(x => new { x.Id, x.KioskId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.AcknowledgedByAccount).WithMany().HasForeignKey(x => x.AcknowledgedByAccountId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class MaintenanceTicketConfiguration : IEntityTypeConfiguration<MaintenanceTicket>
{
    public void Configure(EntityTypeBuilder<MaintenanceTicket> entity)
    {
        entity.ToTable("MaintenanceTickets");
        entity.HasIndex(x => x.TicketNumber).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.Status, x.ReportedAt });
        entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.DeviceEvent).WithMany().HasForeignKey(x => x.DeviceEventId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.AssignedToAccount).WithMany().HasForeignKey(x => x.AssignedToAccountId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.CreatedByAccount).WithMany().HasForeignKey(x => x.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class OperationLogConfiguration : IEntityTypeConfiguration<OperationLog>
{
    public void Configure(EntityTypeBuilder<OperationLog> entity)
    {
        entity.ToTable("OperationLogs");
        entity.HasIndex(x => new { x.KioskId, x.OccurredAt });
        entity.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);

    }
}
