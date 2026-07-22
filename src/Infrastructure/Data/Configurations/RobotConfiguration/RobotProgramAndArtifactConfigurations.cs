using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.RobotConfiguration.ArtifactTemplates.Queries;
using Application.RobotConfiguration.ArtifactTemplates.Commands;
using Domain.RobotConfiguration.ArtifactTemplates;
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
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.AuthoringImports;
using Domain.SalesCatalog.Entities;
using Domain.Sync.DeadLetters;
using Domain.Sync.Entities;
using Domain.Sync.Ingestion;
using Domain.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Infrastructure.Data.Configurations.RobotConfiguration;

internal sealed class RobotProgramConfiguration : IEntityTypeConfiguration<RobotProgram>
{
    public void Configure(EntityTypeBuilder<RobotProgram> entity)
    {
        entity.ToTable("RobotPrograms");
        entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.DeviceId, x.Code }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Store>().WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Kiosk>().WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Device>().WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        entity.Property(x => x.ProgramManifestJson).HasColumnType("jsonb");
        entity.HasIndex(x => x.ProgramManifestChecksum).IsUnique().HasFilter("\"ProgramManifestChecksum\" IS NOT NULL");
        entity.Navigation(x => x.RobotProgramArtifacts).UsePropertyAccessMode(PropertyAccessMode.Field);

    }
}

internal sealed class RobotProgramArtifactConfiguration : IEntityTypeConfiguration<RobotProgramArtifact>
{
    public void Configure(EntityTypeBuilder<RobotProgramArtifact> entity)
    {
        entity.ToTable("RobotProgramArtifacts");
        entity.HasIndex(x => new { x.RobotProgramId, x.RunOrder }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => x.RobotArtifactId);
        entity.Property(x => x.RequiredOptionCode).HasMaxLength(100);
        entity.HasOne(x => x.RobotProgram).WithMany(x => x.RobotProgramArtifacts).HasForeignKey(x => x.RobotProgramId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<RobotArtifact>().WithMany().HasForeignKey(x => x.RobotArtifactId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class RobotArtifactConfiguration : IEntityTypeConfiguration<RobotArtifact>
{
    public void Configure(EntityTypeBuilder<RobotArtifact> entity)
    {
        entity.ToTable("RobotArtifacts");
        entity.HasIndex(x => new { x.OrganizationId, x.ArtifactCode, x.Checksum }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => x.StorageKey).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.RuntimeTargetCode, x.MachineModelCode, x.Status });
        entity.Property(x => x.TechnicalContractChecksum).HasMaxLength(64);
        entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<RobotArtifactTemplate>().WithMany().HasForeignKey(x => x.SourceRobotArtifactTemplateId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<RobotArtifactTechnicalContract>().WithMany().HasForeignKey(x => x.TechnicalContractId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class RobotArtifactTemplateConfiguration : IEntityTypeConfiguration<RobotArtifactTemplate>
{
    public void Configure(EntityTypeBuilder<RobotArtifactTemplate> entity)
    {
        entity.ToTable("RobotArtifactTemplates");
        entity.HasIndex(x => new { x.TemplateCode, x.Checksum }).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => x.StorageKey).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.RuntimeTargetCode, x.MachineModelCode, x.Status });
        entity.Property(x => x.TechnicalContractChecksum).HasMaxLength(64);
        entity.HasOne<RobotArtifactTechnicalContract>().WithMany().HasForeignKey(x => x.TechnicalContractId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class RobotAuthoringImportConfiguration : IEntityTypeConfiguration<RobotAuthoringImport>
{
    public void Configure(EntityTypeBuilder<RobotAuthoringImport> entity)
    {
        entity.ToTable("RobotAuthoringImports");
        entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.OrganizationId, x.ClientExportId, x.ImportChecksum });
        entity.HasIndex(x => new { x.Status, x.CreatedAt });
        entity.Property(x => x.IdempotencyKey).HasMaxLength(200);
        entity.Property(x => x.ImportChecksum).HasMaxLength(64);
        entity.Property(x => x.ValidationReportJson).HasColumnType("jsonb");
        entity.Property(x => x.ComposedOptionCodesJson).HasColumnType("jsonb");
        entity.Property(x => x.CompositionPreviewChecksum).HasMaxLength(64);
        entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Store>().WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Kiosk>().WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Device>().WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<RobotProgram>().WithMany().HasForeignKey(x => x.AppliedRobotProgramId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Recipe>().WithMany().HasForeignKey(x => x.ComposedRecipeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ConfigurationRelease>().WithMany().HasForeignKey(x => x.LinkedConfigurationReleaseId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class RobotAuthoringImportItemConfiguration : IEntityTypeConfiguration<RobotAuthoringImportItem>
{
    public void Configure(EntityTypeBuilder<RobotAuthoringImportItem> entity)
    {
        entity.ToTable("RobotAuthoringImportItems");
        entity.HasIndex(x => new { x.RobotAuthoringImportId, x.RunOrder }).IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.RobotAuthoringImportId, x.ArtifactCode }).IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.Property(x => x.LuaChecksum).HasMaxLength(64);
        entity.Property(x => x.SidecarChecksum).HasMaxLength(64);
        entity.HasOne(x => x.RobotAuthoringImport).WithMany(x => x.Items)
            .HasForeignKey(x => x.RobotAuthoringImportId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<RobotArtifact>().WithMany().HasForeignKey(x => x.RobotArtifactId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<RobotArtifactTechnicalContract>().WithMany().HasForeignKey(x => x.TechnicalContractId).OnDelete(DeleteBehavior.Restrict);
    }
}
