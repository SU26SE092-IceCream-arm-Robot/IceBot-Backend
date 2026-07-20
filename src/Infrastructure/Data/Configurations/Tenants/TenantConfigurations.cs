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
using Domain.ProductionPackages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Infrastructure.Data.Configurations.Tenants;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> entity)
    {
        entity.ToTable("Organizations");
        entity.HasIndex(x => x.Code).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);

    }
}

internal sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> entity)
    {
        entity.ToTable("Stores");
        entity.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne(x => x.Organization)
            .WithMany(x => x.Stores)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class KioskConfiguration : IEntityTypeConfiguration<Kiosk>
{
    public void Configure(EntityTypeBuilder<Kiosk> entity)
    {
        entity.ToTable("Kiosks");
        entity.HasIndex(x => new { x.Id, x.OrganizationId }).IsUnique();
        entity.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => x.SerialNumber).IsUnique().HasFilter(EfModelConfigurationConstants.NotNullAndActive(nameof(Kiosk.SerialNumber)));
        entity.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Store)
            .WithMany(x => x.Kiosks)
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class FranchiseOnboardingConfiguration : IEntityTypeConfiguration<FranchiseOnboarding>
{
    public void Configure(EntityTypeBuilder<FranchiseOnboarding> entity)
    {
        entity.ToTable("FranchiseOnboardings");
        entity.Property(x => x.IdempotencyKey).HasMaxLength(200);
        entity.Property(x => x.RequestChecksum).HasMaxLength(64);
        entity.Property(x => x.RequestJson).Metadata.SetMaxLength(null);
        entity.Property(x => x.FailureCode).HasMaxLength(100);
        entity.Property(x => x.FailureMessage).HasMaxLength(1000);
        entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey })
            .IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.OrganizationId, x.Status, x.UpdatedAt });
        entity.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Store>()
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Kiosk>()
            .WithMany()
            .HasForeignKey(x => x.KioskId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProductionPackageInstallation>()
            .WithMany()
            .HasForeignKey(x => x.PackageInstallationId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => x.PackageInstallationId)
            .IsUnique()
            .HasFilter(EfModelConfigurationConstants.NotNullAndActive(nameof(FranchiseOnboarding.PackageInstallationId)));
    }
}
