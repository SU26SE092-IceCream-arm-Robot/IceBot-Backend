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

internal sealed class KioskConfigurationDeploymentConfiguration : IEntityTypeConfiguration<KioskConfigurationDeployment>
{
    public void Configure(EntityTypeBuilder<KioskConfigurationDeployment> entity)
    {
        entity.ToTable("KioskConfigurationDeployments");
        entity.HasIndex(x => new { x.KioskId, x.ConfigurationReleaseId, x.AttemptNo }).IsUnique();
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.IdempotencyKey }).IsUnique();
        entity.Property(x => x.IdempotencyKey).HasMaxLength(200);
        entity.Property(x => x.WarningCodesJson).HasColumnType("jsonb");
        entity.Property(x => x.ValidationReportChecksum).HasMaxLength(64);
        entity.Property(x => x.RiskLevel).HasMaxLength(50);
        entity.HasIndex(x => new { x.KioskId, x.Status, x.RequestedAt });
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Status });
        entity.HasIndex(x => x.KioskId).IsUnique().HasFilter("\"Status\" IN (1, 2)");
        entity.HasOne(x => x.ConfigurationRelease)
            .WithMany()
            .HasForeignKey(x => new { x.ConfigurationReleaseId, x.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.KioskExecutionEndpoint)
            .WithMany()
            .HasForeignKey(x => new { x.KioskExecutionEndpointId, x.KioskId })
            .HasPrincipalKey(x => new { x.Id, x.KioskId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Kiosk>().WithMany()
            .HasForeignKey(x => new { x.KioskId, x.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Account>().WithMany().HasForeignKey(x => x.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Account>().WithMany().HasForeignKey(x => x.RiskAcknowledgedByAccountId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class ControllerArtifactSetDeploymentConfiguration : IEntityTypeConfiguration<ControllerArtifactSetDeployment>
{
    public void Configure(EntityTypeBuilder<ControllerArtifactSetDeployment> entity)
    {
        entity.ToTable("ControllerArtifactSetDeployments");
        entity.HasIndex(x => new { x.ControllerId, x.ActiveSetVersion }).IsUnique();
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.IdempotencyKey }).IsUnique();
        entity.Property(x => x.IdempotencyKey).HasMaxLength(200);
        entity.Property(x => x.WarningCodesJson).HasColumnType("jsonb");
        entity.Property(x => x.ValidationReportChecksum).HasMaxLength(64);
        entity.Property(x => x.RiskLevel).HasMaxLength(50);
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Status, x.RequestedAt });
        entity.HasIndex(x => new { x.ControllerId, x.LastControllerReportId }).IsUnique().HasFilter("\"LastControllerReportId\" IS NOT NULL");
        entity.HasOne(x => x.SourceConfigurationRelease).WithMany()
            .HasForeignKey(x => new { x.SourceConfigurationReleaseId, x.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.KioskExecutionEndpoint).WithMany()
            .HasForeignKey(x => new { x.KioskExecutionEndpointId, x.KioskId })
            .HasPrincipalKey(x => new { x.Id, x.KioskId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Kiosk>().WithMany()
            .HasForeignKey(x => new { x.KioskId, x.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Account>().WithMany().HasForeignKey(x => x.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Account>().WithMany().HasForeignKey(x => x.RiskAcknowledgedByAccountId).OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

    }
}

internal sealed class ControllerArtifactSetItemConfiguration : IEntityTypeConfiguration<ControllerArtifactSetItem>
{
    public void Configure(EntityTypeBuilder<ControllerArtifactSetItem> entity)
    {
        entity.ToTable("ControllerArtifactSetItems");
        entity.HasIndex(x => new { x.ControllerArtifactSetDeploymentId, x.ExecutionRouteId, x.RobotProgramId, x.RunOrder, x.RobotArtifactId }).IsUnique();
        entity.Property(x => x.ParametersJson).HasColumnType("jsonb");
        entity.Property(x => x.RequiredOptionCode).HasMaxLength(100);
        entity.HasOne(x => x.ControllerArtifactSetDeployment).WithMany(x => x.Items).HasForeignKey(x => x.ControllerArtifactSetDeploymentId).OnDelete(DeleteBehavior.Cascade);

    }
}
