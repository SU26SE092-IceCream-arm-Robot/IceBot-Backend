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
namespace Infrastructure.Data.Configurations.Catalog;

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> entity)
    {
        entity.ToTable("ProductCategories");
        entity.HasIndex(x => x.Code).IsUnique();
        entity.HasOne(x => x.ParentCategory)
            .WithMany(x => x.ChildCategories)
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("Products");
        entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.Code }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.TemplateProduct).WithMany().HasForeignKey(x => x.TemplateProductId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(x => x.OptionGroups).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> entity)
    {
        entity.ToTable("ProductVariants");
        entity.HasIndex(x => new { x.ProductId, x.Code }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.ProductId, x.DisplayOrder });
        entity.HasOne(x => x.Product).WithMany(x => x.ProductVariants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class OptionGroupConfiguration : IEntityTypeConfiguration<OptionGroup>
{
    public void Configure(EntityTypeBuilder<OptionGroup> entity)
    {
        entity.ToTable("OptionGroups");
        entity.HasIndex(x => new { x.ProductId, x.Code }).IsUnique();

    }
}

internal sealed class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> entity)
    {
        entity.ToTable("ProductOptions");
        entity.HasIndex(x => new { x.OptionGroupId, x.Code }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => x.OptionGroupId)
            .IsUnique()
            .HasFilter("\"IsDefault\" = TRUE AND \"DeletedAt\" IS NULL");
        entity.HasOne(x => x.OptionGroup).WithMany(x => x.ProductOptions).HasForeignKey(x => x.OptionGroupId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(x => x.IngredientRequirements).WithOne(x => x.ProductOption).HasForeignKey(x => x.ProductOptionId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class ProductOptionIngredientRequirementConfiguration : IEntityTypeConfiguration<ProductOptionIngredientRequirement>
{
    public void Configure(EntityTypeBuilder<ProductOptionIngredientRequirement> entity)
    {
        entity.ToTable("ProductOptionIngredientRequirements");
        entity.HasIndex(x => new { x.ProductOptionId, x.IngredientId }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne<Ingredient>().WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);
    }
}
