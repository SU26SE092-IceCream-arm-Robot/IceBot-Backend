using Domain.ProductionPackages;
using Domain.Catalog.Entities;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.ArtifactTemplates;
using Domain.RobotConfiguration.Programs;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.ProductionPackages;

internal sealed class ProductionPackageConfiguration : IEntityTypeConfiguration<ProductionPackage>
{
    public void Configure(EntityTypeBuilder<ProductionPackage> entity)
    {
        entity.ToTable("ProductionPackages");
        entity.Property(x => x.Code).HasMaxLength(100);
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.HasIndex(x => x.Code).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        entity.Navigation(x => x.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProductionPackageVersionConfiguration : IEntityTypeConfiguration<ProductionPackageVersion>
{
    public void Configure(EntityTypeBuilder<ProductionPackageVersion> entity)
    {
        entity.ToTable("ProductionPackageVersions");
        entity.HasIndex(x => new { x.ProductionPackageId, x.Version }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        entity.HasIndex(x => x.ManifestChecksum).IsUnique().HasFilter("\"ManifestChecksum\" IS NOT NULL AND \"DeletedAt\" IS NULL");
        entity.Property(x => x.ManifestChecksum).HasMaxLength(64);
        entity.Property(x => x.ManifestJson).HasColumnType("jsonb");
        entity.HasOne(x => x.ProductionPackage).WithMany(x => x.Versions).HasForeignKey(x => x.ProductionPackageId).OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(x => x.Products).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(x => x.Artifacts).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(x => x.Programs).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(x => x.Routes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProductionPackageProductDefinitionConfiguration : IEntityTypeConfiguration<ProductionPackageProductDefinition>
{
    public void Configure(EntityTypeBuilder<ProductionPackageProductDefinition> entity)
    {
        entity.ToTable("ProductionPackageProductDefinitions");
        entity.Property(x => x.ProductSnapshotJson).HasColumnType("jsonb");
        entity.Property(x => x.SourceKey).HasMaxLength(100);
        entity.Property(x => x.ProductSnapshotChecksum).HasMaxLength(64);
        entity.HasIndex(x => new { x.PackageVersionId, x.SourceKey }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        entity.HasOne(x => x.PackageVersion).WithMany(x => x.Products).HasForeignKey(x => x.PackageVersionId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Product>().WithMany().HasForeignKey(x => x.SourceProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductionPackageArtifactDefinitionConfiguration : IEntityTypeConfiguration<ProductionPackageArtifactDefinition>
{
    public void Configure(EntityTypeBuilder<ProductionPackageArtifactDefinition> entity)
    {
        entity.ToTable("ProductionPackageArtifactDefinitions");
        entity.Property(x => x.SourceKey).HasMaxLength(100);
        entity.Property(x => x.ArtifactChecksum).HasMaxLength(64);
        entity.Property(x => x.TechnicalContractChecksum).HasMaxLength(64);
        entity.HasIndex(x => new { x.PackageVersionId, x.SourceKey }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        entity.HasOne(x => x.PackageVersion).WithMany(x => x.Artifacts).HasForeignKey(x => x.PackageVersionId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<RobotArtifactTemplate>().WithMany().HasForeignKey(x => x.RobotArtifactTemplateId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<RobotArtifactTechnicalContract>().WithMany().HasForeignKey(x => x.TechnicalContractId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductionPackageProgramBlueprintConfiguration : IEntityTypeConfiguration<ProductionPackageProgramBlueprint>
{
    public void Configure(EntityTypeBuilder<ProductionPackageProgramBlueprint> entity)
    {
        entity.ToTable("ProductionPackageProgramBlueprints");
        entity.Property(x => x.BlueprintCode).HasMaxLength(100);
        entity.Property(x => x.RuntimeTargetCode).HasMaxLength(100);
        entity.Property(x => x.MachineModelCode).HasMaxLength(100);
        entity.HasIndex(x => new { x.PackageVersionId, x.BlueprintCode }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        entity.HasOne(x => x.PackageVersion).WithMany(x => x.Programs).HasForeignKey(x => x.PackageVersionId).OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(x => x.Slots).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProductionPackageProgramSlotConfiguration : IEntityTypeConfiguration<ProductionPackageProgramSlot>
{
    public void Configure(EntityTypeBuilder<ProductionPackageProgramSlot> entity)
    {
        entity.ToTable("ProductionPackageProgramSlots");
        entity.Property(x => x.SlotCode).HasMaxLength(100);
        entity.Property(x => x.ArtifactSourceKey).HasMaxLength(100);
        entity.Property(x => x.RequiredEffectCode).HasMaxLength(100);
        entity.Property(x => x.Phase).HasMaxLength(100);
        entity.HasIndex(x => new { x.ProgramBlueprintId, x.SlotCode }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        entity.HasOne(x => x.ProgramBlueprint).WithMany(x => x.Slots).HasForeignKey(x => x.ProgramBlueprintId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductionPackageRouteBlueprintConfiguration : IEntityTypeConfiguration<ProductionPackageRouteBlueprint>
{
    public void Configure(EntityTypeBuilder<ProductionPackageRouteBlueprint> entity)
    {
        entity.ToTable("ProductionPackageRouteBlueprints");
        entity.Property(x => x.RouteCode).HasMaxLength(100);
        entity.Property(x => x.ProductSourceKey).HasMaxLength(100);
        entity.Property(x => x.ProductVariantSourceKey).HasMaxLength(100);
        entity.Property(x => x.RecipeSourceKey).HasMaxLength(100);
        entity.Property(x => x.SupportedOptionCodesJson).HasColumnType("jsonb");
        entity.Property(x => x.ProgramBlueprintCode).HasMaxLength(100);
        entity.Property(x => x.RequiredCapabilitiesJson).HasColumnType("jsonb");
        entity.HasIndex(x => new { x.PackageVersionId, x.RouteCode }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        entity.HasOne(x => x.PackageVersion).WithMany(x => x.Routes).HasForeignKey(x => x.PackageVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductionPackageInstallationConfiguration : IEntityTypeConfiguration<ProductionPackageInstallation>
{
    public void Configure(EntityTypeBuilder<ProductionPackageInstallation> entity)
    {
        entity.ToTable("ProductionPackageInstallations");
        entity.Property(x => x.PackageManifestChecksum).HasMaxLength(64);
        entity.Property(x => x.RequestChecksum).HasMaxLength(64);
        entity.Property(x => x.SelectedProductSourceKeysJson).HasColumnType("jsonb");
        entity.Property(x => x.IdempotencyKey).HasMaxLength(200);
        entity.Property(x => x.MaterializationIdentitySuffix).HasMaxLength(40);
        entity.Property(x => x.FailureCode).HasMaxLength(100);
        entity.Property(x => x.FailureMessage).HasMaxLength(2000);
        entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        entity.HasIndex(x => new { x.OrganizationId, x.Status, x.StartedAt });
        entity.HasOne(x => x.PackageVersion).WithMany().HasForeignKey(x => x.PackageVersionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Store>().WithMany().HasForeignKey(x => new { x.StoreId, x.OrganizationId })
            .HasPrincipalKey(x => new { StoreId = (Guid?)x.Id, x.OrganizationId }).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Kiosk>().WithMany().HasForeignKey(x => new { x.KioskId, x.OrganizationId })
            .HasPrincipalKey(x => new { KioskId = (Guid?)x.Id, x.OrganizationId }).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ConfigurationRelease>().WithMany().HasForeignKey(x => x.DraftConfigurationReleaseId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(table => table.HasCheckConstraint("CK_ProductionPackageInstallations_KioskRequiresStore",
            "\"KioskId\" IS NULL OR \"StoreId\" IS NOT NULL"));
        entity.Navigation(x => x.Materializations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProductionPackageUpgradeConfiguration : IEntityTypeConfiguration<ProductionPackageUpgrade>
{
    public void Configure(EntityTypeBuilder<ProductionPackageUpgrade> entity)
    {
        entity.ToTable("ProductionPackageUpgrades");
        entity.Property(x => x.PreviewChecksum).HasMaxLength(64);
        entity.Property(x => x.SourceManifestChecksum).HasMaxLength(64);
        entity.Property(x => x.TargetManifestChecksum).HasMaxLength(64);
        entity.Property(x => x.SelectedProductSourceKeysJson).HasColumnType("jsonb");
        entity.Property(x => x.IdempotencyKey).HasMaxLength(200);
        entity.Property(x => x.FailureCode).HasMaxLength(100);
        entity.Property(x => x.FailureMessage).HasMaxLength(2000);
        entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => new { x.OrganizationId, x.SourceInstallationId, x.Status });
        entity.HasIndex(x => new { x.OrganizationId, x.SourceInstallationId }).IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL AND \"Status\" IN (0, 1, 2, 3)");
        entity.HasOne(x => x.SourceInstallation).WithMany().HasForeignKey(x => x.SourceInstallationId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.TargetPackageVersion).WithMany().HasForeignKey(x => x.TargetPackageVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.TargetInstallation).WithMany().HasForeignKey(x => x.TargetInstallationId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(x => x.MenuChanges).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(x => x.EndpointTargets).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(x => x.CatalogIdentityChanges).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(x => x.AvailabilityChanges).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProductionPackageUpgradeAvailabilityChangeConfiguration : IEntityTypeConfiguration<ProductionPackageUpgradeAvailabilityChange>
{
    public void Configure(EntityTypeBuilder<ProductionPackageUpgradeAvailabilityChange> entity)
    {
        entity.ToTable("ProductionPackageUpgradeAvailabilityChanges");
        entity.Property(x => x.ResourceSourceKey).HasMaxLength(300);
        entity.HasIndex(x => new { x.UpgradeId, x.ResourceKind, x.SourceResourceId }).IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne(x => x.Upgrade).WithMany(x => x.AvailabilityChanges).HasForeignKey(x => x.UpgradeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductionPackageUpgradeMenuChangeConfiguration : IEntityTypeConfiguration<ProductionPackageUpgradeMenuChange>
{
    public void Configure(EntityTypeBuilder<ProductionPackageUpgradeMenuChange> entity)
    {
        entity.ToTable("ProductionPackageUpgradeMenuChanges");
        entity.Property(x => x.BeforeBindingChecksum).HasMaxLength(64);
        entity.Property(x => x.AfterBindingChecksum).HasMaxLength(64);
        entity.HasIndex(x => new { x.UpgradeId, x.MenuItemId }).IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne(x => x.Upgrade).WithMany(x => x.MenuChanges).HasForeignKey(x => x.UpgradeId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Menu>().WithMany().HasForeignKey(x => x.MenuId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<MenuItem>().WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(x => x.OptionChanges).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProductionPackageUpgradeMenuOptionChangeConfiguration : IEntityTypeConfiguration<ProductionPackageUpgradeMenuOptionChange>
{
    public void Configure(EntityTypeBuilder<ProductionPackageUpgradeMenuOptionChange> entity)
    {
        entity.ToTable("ProductionPackageUpgradeMenuOptionChanges");
        entity.Property(x => x.OptionSourceKey).HasMaxLength(300);
        entity.HasIndex(x => new { x.UpgradeMenuChangeId, x.OptionSourceKey }).IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne(x => x.UpgradeMenuChange).WithMany(x => x.OptionChanges)
            .HasForeignKey(x => x.UpgradeMenuChangeId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductionPackageUpgradeEndpointTargetConfiguration : IEntityTypeConfiguration<ProductionPackageUpgradeEndpointTarget>
{
    public void Configure(EntityTypeBuilder<ProductionPackageUpgradeEndpointTarget> entity)
    {
        entity.ToTable("ProductionPackageUpgradeEndpointTargets");
        entity.HasIndex(x => new { x.UpgradeId, x.KioskExecutionEndpointId }).IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne(x => x.Upgrade).WithMany(x => x.EndpointTargets).HasForeignKey(x => x.UpgradeId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<KioskExecutionEndpoint>().WithMany().HasForeignKey(x => x.KioskExecutionEndpointId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(x => x.RollbackAttempts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProductionPackageUpgradeRollbackAttemptConfiguration : IEntityTypeConfiguration<ProductionPackageUpgradeRollbackAttempt>
{
    public void Configure(EntityTypeBuilder<ProductionPackageUpgradeRollbackAttempt> entity)
    {
        entity.ToTable("ProductionPackageUpgradeRollbackAttempts");
        entity.Property(x => x.Reason).HasMaxLength(500);
        entity.HasIndex(x => new { x.UpgradeEndpointTargetId, x.AttemptNo }).IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => x.DeploymentId).IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne(x => x.UpgradeEndpointTarget).WithMany(x => x.RollbackAttempts)
            .HasForeignKey(x => x.UpgradeEndpointTargetId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductionPackageUpgradeCatalogIdentityChangeConfiguration : IEntityTypeConfiguration<ProductionPackageUpgradeCatalogIdentityChange>
{
    public void Configure(EntityTypeBuilder<ProductionPackageUpgradeCatalogIdentityChange> entity)
    {
        entity.ToTable("ProductionPackageUpgradeCatalogIdentityChanges");
        entity.Property(x => x.ProductSourceKey).HasMaxLength(100);
        entity.Property(x => x.SourceCodeBefore).HasMaxLength(100);
        entity.Property(x => x.SourceCodeAfter).HasMaxLength(100);
        entity.Property(x => x.TargetCodeBefore).HasMaxLength(100);
        entity.Property(x => x.TargetCodeAfter).HasMaxLength(100);
        entity.Property(x => x.BeforeChecksum).HasMaxLength(64);
        entity.Property(x => x.AfterChecksum).HasMaxLength(64);
        entity.HasIndex(x => new { x.UpgradeId, x.ProductSourceKey }).IsUnique()
            .HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasOne(x => x.Upgrade).WithMany(x => x.CatalogIdentityChanges).HasForeignKey(x => x.UpgradeId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Product>().WithMany().HasForeignKey(x => x.SourceProductId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Product>().WithMany().HasForeignKey(x => x.TargetProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductionPackageMaterializationConfiguration : IEntityTypeConfiguration<ProductionPackageMaterialization>
{
    public void Configure(EntityTypeBuilder<ProductionPackageMaterialization> entity)
    {
        entity.ToTable("ProductionPackageMaterializations");
        entity.Property(x => x.SourceKey).HasMaxLength(300);
        entity.Property(x => x.TargetKey).HasMaxLength(200);
        entity.Property(x => x.TargetChecksum).HasMaxLength(64);
        entity.HasIndex(x => new { x.InstallationId, x.ResourceKind, x.SourceKey }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        entity.HasIndex(x => new { x.ResourceKind, x.TargetKey }).HasFilter("\"DeletedAt\" IS NULL");
        entity.HasOne(x => x.Installation).WithMany(x => x.Materializations).HasForeignKey(x => x.InstallationId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductionCompositionConfiguration : IEntityTypeConfiguration<ProductionComposition>
{
    public void Configure(EntityTypeBuilder<ProductionComposition> entity)
    {
        entity.ToTable("ProductionCompositions");
        entity.Property(x => x.InputJson).HasColumnType("jsonb");
        entity.Property(x => x.ReportJson).HasColumnType("jsonb");
        entity.Property(x => x.RuntimeTargetCode).HasMaxLength(100);
        entity.Property(x => x.MachineModelCode).HasMaxLength(100);
        entity.Property(x => x.InputChecksum).HasMaxLength(64);
        entity.HasIndex(x => new { x.InstallationId, x.InputChecksum }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        entity.HasIndex(x => new { x.OrganizationId, x.ProductVariantId, x.Status });
        entity.HasOne<ProductionPackageInstallation>().WithMany().HasForeignKey(x => x.InstallationId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Recipe>().WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<KioskExecutionEndpoint>().WithMany().HasForeignKey(x => x.TargetExecutionEndpointId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<RobotProgram>().WithMany().HasForeignKey(x => x.GeneratedRobotProgramId).OnDelete(DeleteBehavior.Restrict);
    }
}
