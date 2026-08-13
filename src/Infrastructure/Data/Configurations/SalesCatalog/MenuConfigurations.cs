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
namespace Infrastructure.Data.Configurations.SalesCatalog;

internal sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> entity)
    {
        entity.ToTable("Menus");
        entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.Code }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.Status });
        entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> entity)
    {
        entity.ToTable("MenuItems");
        entity.HasIndex(x => new { x.MenuId, x.Code }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.MenuId, x.Status, x.DisplayOrder });
        entity.HasOne(x => x.Menu).WithMany(x => x.MenuItems).HasForeignKey(x => x.MenuId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(x => x.ProductOptions).WithOne(x => x.MenuItem).HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class MenuItemProductOptionConfiguration : IEntityTypeConfiguration<MenuItemProductOption>
{
    public void Configure(EntityTypeBuilder<MenuItemProductOption> entity)
    {
        entity.ToTable("MenuItemProductOptions");
        entity.HasIndex(x => new { x.MenuItemId, x.ProductOptionId }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne<ProductOption>().WithMany().HasForeignKey(x => x.ProductOptionId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class KioskMenuItemAvailabilityConfiguration : IEntityTypeConfiguration<KioskMenuItemAvailability>
{
    public void Configure(EntityTypeBuilder<KioskMenuItemAvailability> entity)
    {
        entity.ToTable("KioskMenuItemAvailabilities");
        entity.Property(x => x.Reason).HasMaxLength(1000);
        entity.Property(x => x.Revision).IsConcurrencyToken();
        entity.HasIndex(x => new { x.KioskId, x.MenuItemId }).IsUnique();
        entity.HasIndex(x => new { x.KioskId, x.State });
        entity.HasOne<Kiosk>().WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Menu>().WithMany().HasForeignKey(x => x.MenuId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<MenuItem>().WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class KioskMenuItemAvailabilityTransitionConfiguration : IEntityTypeConfiguration<KioskMenuItemAvailabilityTransition>
{
    public void Configure(EntityTypeBuilder<KioskMenuItemAvailabilityTransition> entity)
    {
        entity.ToTable("KioskMenuItemAvailabilityTransitions");
        entity.Property(x => x.Reason).HasMaxLength(1000);
        entity.Property(x => x.ActorRoleCodeSnapshot).HasMaxLength(100);
        entity.Property(x => x.RequestId).HasMaxLength(200);
        entity.HasIndex(x => new { x.AvailabilityId, x.OccurredAt });
        entity.HasIndex(x => new { x.AvailabilityId, x.RequestId }).IsUnique();
        entity.HasOne<KioskMenuItemAvailability>().WithMany(x => x.Transitions)
            .HasForeignKey(x => x.AvailabilityId).OnDelete(DeleteBehavior.Restrict);
    }
}
