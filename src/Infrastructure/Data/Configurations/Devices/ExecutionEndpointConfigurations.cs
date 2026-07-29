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

internal sealed class KioskExecutionEndpointConfiguration : IEntityTypeConfiguration<KioskExecutionEndpoint>
{
    public void Configure(EntityTypeBuilder<KioskExecutionEndpoint> entity)
    {
        entity.ToTable("KioskExecutionEndpoints", table =>
            table.HasCheckConstraint(
                "CK_KioskExecutionEndpoints_ProfileIdentity",
                "((\"ExecutionProfile\" = 1 AND \"ControllerId\" IS NULL) OR (\"ExecutionProfile\" = 2 AND \"FullEdgeRuntimeId\" IS NULL)) AND (\"Status\" <> 2 OR ((\"ExecutionProfile\" = 1 AND \"FullEdgeRuntimeId\" IS NOT NULL) OR (\"ExecutionProfile\" = 2 AND \"ControllerId\" IS NOT NULL)))"));
        entity.HasIndex(x => new { x.KioskId, x.EndpointCode }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.Id, x.KioskId }).IsUnique();
        entity.HasIndex(x => x.FullEdgeRuntimeId).IsUnique().HasFilter("\"FullEdgeRuntimeId\" IS NOT NULL");
        entity.HasIndex(x => x.ControllerId).IsUnique().HasFilter("\"ControllerId\" IS NOT NULL");
        entity.HasIndex(x => x.CredentialBindingId).IsUnique().HasFilter("\"CredentialBindingId\" IS NOT NULL");
        entity.HasIndex(x => x.LastEdgeActivationEventId).IsUnique().HasFilter("\"LastEdgeActivationEventId\" IS NOT NULL");
        entity.HasIndex(x => x.LastControllerActivationReportId).IsUnique().HasFilter("\"LastControllerActivationReportId\" IS NOT NULL");
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.CredentialBinding)
            .WithMany()
            .HasForeignKey(x => x.CredentialBindingId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(x => x.SupportedRobotTargets)
            .WithOne(x => x.KioskExecutionEndpoint)
            .HasForeignKey(x => new { x.KioskExecutionEndpointId, x.KioskId })
            .HasPrincipalKey(x => new { x.Id, x.KioskId })
            .OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(x => x.SupportedRobotTargets).UsePropertyAccessMode(PropertyAccessMode.Field);

    }
}

internal sealed class ExecutionEndpointCredentialBindingConfiguration : IEntityTypeConfiguration<ExecutionEndpointCredentialBinding>
{
    public void Configure(EntityTypeBuilder<ExecutionEndpointCredentialBinding> entity)
    {
        entity.ToTable("ExecutionEndpointCredentialBindings");
        entity.HasIndex(x => x.CredentialReference).IsUnique();
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Status });
        entity.HasOne(x => x.KioskExecutionEndpoint)
            .WithMany()
            .HasForeignKey(x => x.KioskExecutionEndpointId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class ExecutionEndpointMqttCredentialConfiguration : IEntityTypeConfiguration<ExecutionEndpointMqttCredential>
{
    public void Configure(EntityTypeBuilder<ExecutionEndpointMqttCredential> entity)
    {
        entity.ToTable("ExecutionEndpointMqttCredentials");
        entity.HasIndex(x => x.KioskExecutionEndpointId).IsUnique();
        entity.HasIndex(x => x.Username).IsUnique();
        entity.Property(x => x.Username).HasMaxLength(100);
        entity.Property(x => x.BrokerProvider).HasMaxLength(100);
        entity.Property(x => x.LastError).HasMaxLength(1000);
        entity.HasOne(x => x.KioskExecutionEndpoint)
            .WithOne(x => x.MqttCredential)
            .HasForeignKey<ExecutionEndpointMqttCredential>(x => x.KioskExecutionEndpointId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class ExecutionEndpointReadinessProjectionConfiguration : IEntityTypeConfiguration<ExecutionEndpointReadinessProjection>
{
    public void Configure(EntityTypeBuilder<ExecutionEndpointReadinessProjection> entity)
    {
        entity.ToTable("ExecutionEndpointReadinessProjections");
        entity.HasIndex(x => x.KioskExecutionEndpointId).IsUnique();
        entity.HasIndex(x => new { x.KioskId, x.Readiness, x.Activity });
        entity.Property(x => x.FaultCode).HasMaxLength(100);
        entity.Navigation(x => x.Capabilities).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.KioskExecutionEndpoint).WithOne()
            .HasForeignKey<ExecutionEndpointReadinessProjection>(x => new { x.KioskExecutionEndpointId, x.KioskId })
            .HasPrincipalKey<KioskExecutionEndpoint>(x => new { x.Id, x.KioskId })
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class ExecutionEndpointCapabilityProjectionConfiguration : IEntityTypeConfiguration<ExecutionEndpointCapabilityProjection>
{
    public void Configure(EntityTypeBuilder<ExecutionEndpointCapabilityProjection> entity)
    {
        entity.ToTable("ExecutionEndpointCapabilityProjections");
        entity.HasIndex(x => new { x.ExecutionEndpointReadinessProjectionId, x.CapabilityCode }).IsUnique();
        entity.Property(x => x.CapabilityCode).HasMaxLength(100);
        entity.Property(x => x.WorkcellCode).HasMaxLength(100);
        entity.Property(x => x.UnavailableReason).HasMaxLength(500);
        entity.HasOne(x => x.ReadinessProjection).WithMany(x => x.Capabilities)
            .HasForeignKey(x => x.ExecutionEndpointReadinessProjectionId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class ExecutionEndpointRequestNonceConfiguration : IEntityTypeConfiguration<ExecutionEndpointRequestNonce>
{
    public void Configure(EntityTypeBuilder<ExecutionEndpointRequestNonce> entity)
    {
        entity.ToTable("ExecutionEndpointRequestNonces");
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Nonce }).IsUnique();
        entity.HasIndex(x => x.ExpiresAt);
        entity.HasOne(x => x.KioskExecutionEndpoint)
            .WithMany()
            .HasForeignKey(x => x.KioskExecutionEndpointId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class ExecutionEndpointSupportedRobotTargetConfiguration : IEntityTypeConfiguration<ExecutionEndpointSupportedRobotTarget>
{
    public void Configure(EntityTypeBuilder<ExecutionEndpointSupportedRobotTarget> entity)
    {
        entity.ToTable("ExecutionEndpointSupportedRobotTargets");
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.RuntimeTargetCode, x.MachineModelCode })
            .IsUnique()
            .HasFilter("\"DeviceId\" IS NULL");
        entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.RuntimeTargetCode, x.MachineModelCode, x.DeviceId })
            .IsUnique()
            .HasFilter("\"DeviceId\" IS NOT NULL");
        entity.HasOne(x => x.Device).WithMany()
            .HasForeignKey(x => new { x.DeviceId, x.KioskId })
            .HasPrincipalKey(x => new { x.Id, x.KioskId })
            .OnDelete(DeleteBehavior.Restrict);

    }
}
