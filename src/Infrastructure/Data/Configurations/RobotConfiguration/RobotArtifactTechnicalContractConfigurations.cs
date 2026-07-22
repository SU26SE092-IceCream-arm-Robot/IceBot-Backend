using Domain.RobotConfiguration.ArtifactContracts;
using Domain.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.RobotConfiguration;

internal sealed class RobotArtifactTechnicalContractConfiguration : IEntityTypeConfiguration<RobotArtifactTechnicalContract>
{
    public void Configure(EntityTypeBuilder<RobotArtifactTechnicalContract> entity)
    {
        entity.ToTable("RobotArtifactTechnicalContracts");
        entity.Property(x => x.ContractCode).HasMaxLength(100);
        entity.Property(x => x.RuntimeTargetCode).HasMaxLength(100);
        entity.Property(x => x.MachineModelCode).HasMaxLength(100);
        entity.Property(x => x.ContractChecksum).HasMaxLength(64);
        entity.HasIndex(x => new { x.ContractCode, x.ContractVersion }).IsUnique()
            .HasFilter("\"OrganizationId\" IS NULL AND \"DeletedAt\" IS NULL");
        entity.HasIndex(x => new { x.OrganizationId, x.ContractCode, x.ContractVersion }).IsUnique()
            .HasFilter("\"OrganizationId\" IS NOT NULL AND \"DeletedAt\" IS NULL");
        entity.HasIndex(x => x.ContractChecksum).IsUnique().HasFilter("\"ContractChecksum\" IS NOT NULL AND \"DeletedAt\" IS NULL");
        entity.Property(x => x.ContractJson).HasColumnType("jsonb");
        entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<RobotArtifactTechnicalContract>().WithMany().HasForeignKey(x => x.SourceContractId).OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(x => x.Effects).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(x => x.OrderingConstraints).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class RobotArtifactDeclaredEffectConfiguration : IEntityTypeConfiguration<RobotArtifactDeclaredEffect>
{
    public void Configure(EntityTypeBuilder<RobotArtifactDeclaredEffect> entity)
    {
        entity.ToTable("RobotArtifactDeclaredEffects");
        entity.Property(x => x.EffectCode).HasMaxLength(100);
        entity.Property(x => x.IngredientCode).HasMaxLength(100);
        entity.Property(x => x.OptionCode).HasMaxLength(100);
        entity.Property(x => x.Unit).HasMaxLength(50);
        entity.Property(x => x.RequiredWorkcellCapabilityCode).HasMaxLength(100);
        entity.HasIndex(x => new { x.TechnicalContractId, x.EffectCode }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        entity.HasOne(x => x.TechnicalContract).WithMany(x => x.Effects)
            .HasForeignKey(x => x.TechnicalContractId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RobotArtifactOrderingConstraintConfiguration : IEntityTypeConfiguration<RobotArtifactOrderingConstraint>
{
    public void Configure(EntityTypeBuilder<RobotArtifactOrderingConstraint> entity)
    {
        entity.ToTable("RobotArtifactOrderingConstraints");
        entity.Property(x => x.Value).HasMaxLength(100);
        entity.HasIndex(x => new { x.TechnicalContractId, x.ConstraintType, x.Value, x.SortHint }).IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
        entity.HasOne(x => x.TechnicalContract).WithMany(x => x.OrderingConstraints)
            .HasForeignKey(x => x.TechnicalContractId).OnDelete(DeleteBehavior.Cascade);
    }
}
