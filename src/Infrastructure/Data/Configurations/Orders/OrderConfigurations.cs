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
namespace Infrastructure.Data.Configurations.Orders;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.ToTable("Orders");
        entity.HasIndex(x => x.OrderNumber).IsUnique();
        entity.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
        entity.HasIndex(x => new { x.KioskId, x.ClientOrderId }).IsUnique().HasFilter("\"ClientOrderId\" IS NOT NULL");
        entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.PlacedAt });
        entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> entity)
    {
        entity.ToTable("OrderItems");
        entity.HasIndex(x => new { x.OrderId, x.ClientLineId }).IsUnique().HasFilter(EfModelConfigurationConstants.NotNullAndActive(nameof(OrderItem.ClientLineId)));
        entity.HasOne(x => x.Order).WithMany(x => x.OrderItems).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(x => x.Options).WithOne(x => x.OrderItem).HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class OrderItemOptionConfiguration : IEntityTypeConfiguration<OrderItemOption>
{
    public void Configure(EntityTypeBuilder<OrderItemOption> entity)
    {
        entity.ToTable("OrderItemOptions");
        entity.HasIndex(x => new { x.OrderItemId, x.ProductOptionId }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.OrderItemId, x.OptionGroupId });
        entity.HasMany(x => x.IngredientRequirements).WithOne(x => x.OrderItemOption).HasForeignKey(x => x.OrderItemOptionId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class OrderItemOptionIngredientRequirementConfiguration : IEntityTypeConfiguration<OrderItemOptionIngredientRequirement>
{
    public void Configure(EntityTypeBuilder<OrderItemOptionIngredientRequirement> entity)
    {
        entity.ToTable("OrderItemOptionIngredientRequirements");
        entity.HasIndex(x => new { x.OrderItemOptionId, x.IngredientId }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
    }
}

internal sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> entity)
    {
        entity.ToTable("OrderStatusHistories");
        entity.HasIndex(x => new { x.OrderId, x.ChangedAt });
        entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.ChangedByAccount).WithMany().HasForeignKey(x => x.ChangedByAccountId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class OrderItemStatusHistoryConfiguration : IEntityTypeConfiguration<OrderItemStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderItemStatusHistory> entity)
    {
        entity.ToTable("OrderItemStatusHistories");
        entity.HasIndex(x => new { x.OrderItemId, x.ChangedAt });
        entity.Property(x => x.SourcePayloadHash).HasMaxLength(64);
        entity.HasIndex(x => new { x.OrderItemId, x.SourceEventId })
            .IsUnique()
            .HasFilter("\"SourceEventId\" IS NOT NULL");
        entity.HasOne<OrderItem>().WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Account>().WithMany().HasForeignKey(x => x.ChangedByAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
