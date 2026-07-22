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

internal sealed class DeviceTypeConfiguration : IEntityTypeConfiguration<DeviceType>
{
    public void Configure(EntityTypeBuilder<DeviceType> entity)
    {
        entity.ToTable("DeviceTypes");
        entity.HasIndex(x => x.Code).IsUnique();

    }
}

internal sealed class DeviceModelConfiguration : IEntityTypeConfiguration<DeviceModel>
{
    public void Configure(EntityTypeBuilder<DeviceModel> entity)
    {
        entity.ToTable("DeviceModels");
        entity.HasIndex(x => new { x.DeviceTypeId, x.Code }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne(x => x.DeviceType)
            .WithMany(x => x.DeviceModels)
            .HasForeignKey(x => x.DeviceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> entity)
    {
        entity.ToTable("Devices");
        entity.HasIndex(x => new { x.Id, x.KioskId }).IsUnique();
        entity.HasIndex(x => new { x.KioskId, x.Code }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => x.SerialNumber).IsUnique().HasFilter(EfModelConfigurationConstants.NotNullAndActive(nameof(Device.SerialNumber)));
        entity.HasOne(x => x.DeviceType).WithMany().HasForeignKey(x => x.DeviceTypeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.DeviceModel).WithMany().HasForeignKey(x => x.DeviceModelId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany(x => x.Devices).HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);

    }
}
