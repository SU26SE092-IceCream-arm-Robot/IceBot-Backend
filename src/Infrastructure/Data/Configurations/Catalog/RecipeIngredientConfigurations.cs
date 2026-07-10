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

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> entity)
    {
        entity.ToTable("Recipes");
        entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.ProductVariantId, x.Code, x.Version }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => x.ProductVariantId);
        entity.HasIndex(x => x.ProductVariantId, "IX_Recipes_ProductVariantId_Default")
            .IsUnique()
            .HasFilter("\"IsDefault\" = TRUE AND \"Status\" <> 4 AND \"DeletedAt\" IS NULL");
        entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.ProductVariant).WithMany(x => x.Recipes).HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.TemplateRecipe).WithMany().HasForeignKey(x => x.TemplateRecipeId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class RecipeItemConfiguration : IEntityTypeConfiguration<RecipeItem>
{
    public void Configure(EntityTypeBuilder<RecipeItem> entity)
    {
        entity.ToTable("RecipeItems");
        entity.HasIndex(x => new { x.RecipeId, x.IngredientId, x.StepOrder }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne(x => x.Recipe).WithMany(x => x.RecipeItems).HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> entity)
    {
        entity.ToTable("Ingredients");
        entity.HasIndex(x => x.Code).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.Property(x => x.IsActive).HasDefaultValue(true);

    }
}
