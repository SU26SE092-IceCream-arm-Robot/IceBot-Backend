using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
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
namespace Infrastructure.Data.Configurations.ProductionConfiguration;

internal sealed class ConfigurationReleaseConfiguration : IEntityTypeConfiguration<ConfigurationRelease>
{
    public void Configure(EntityTypeBuilder<ConfigurationRelease> entity)
    {
        entity.ToTable("ConfigurationReleases");
        entity.HasIndex(x => new { x.Id, x.OrganizationId }).IsUnique();
        entity.HasIndex(x => new { x.OrganizationId, x.ReleaseNumber })
            .IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.Status, x.PublishedAt });
        entity.HasIndex(x => x.ReleaseChecksum).IsUnique().HasFilter("\"ReleaseChecksum\" IS NOT NULL");
        entity.Property(x => x.ManifestJson).HasColumnType("jsonb");
        entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(x => x.ExecutionRoutes).UsePropertyAccessMode(PropertyAccessMode.Field);

    }
}

internal sealed class ExecutionRouteConfiguration : IEntityTypeConfiguration<ExecutionRoute>
{
    public void Configure(EntityTypeBuilder<ExecutionRoute> entity)
    {
        entity.ToTable("ExecutionRoutes");
        entity.HasIndex(x => new { x.ConfigurationReleaseId, x.RouteCode })
            .IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.ConfigurationReleaseId, x.ProductVariantId, x.RecipeId, x.Priority });
        entity.Property(x => x.RequiredCapabilitiesJson).HasColumnType("jsonb");
        entity.Property(x => x.SupportedOptionCodesJson).HasColumnType("jsonb").HasMaxLength(10000)
            .HasDefaultValueSql("'[]'::jsonb");
        entity.Property(x => x.ProductionDefinitionJson).HasColumnType("jsonb");
        entity.Property(x => x.ProductionDefinitionChecksum).HasMaxLength(64);
        entity.HasIndex(x => x.ProductionDefinitionChecksum)
            .HasFilter("\"ProductionDefinitionChecksum\" IS NOT NULL");
        entity.HasOne(x => x.ConfigurationRelease)
            .WithMany(x => x.ExecutionRoutes)
            .HasForeignKey(x => x.ConfigurationReleaseId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(x => x.RobotBindings).UsePropertyAccessMode(PropertyAccessMode.Field);

    }
}

internal sealed class ExecutionRouteRobotBindingConfiguration : IEntityTypeConfiguration<ExecutionRouteRobotBinding>
{
    public void Configure(EntityTypeBuilder<ExecutionRouteRobotBinding> entity)
    {
        entity.ToTable("ExecutionRouteRobotBindings");
        entity.HasIndex(x => new { x.ExecutionRouteId, x.BindingOrder })
            .IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => x.RobotProgramId);
        entity.HasOne(x => x.ExecutionRoute)
            .WithMany(x => x.RobotBindings)
            .HasForeignKey(x => x.ExecutionRouteId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.RobotProgram).WithMany().HasForeignKey(x => x.RobotProgramId).OnDelete(DeleteBehavior.Restrict);

    }
}
