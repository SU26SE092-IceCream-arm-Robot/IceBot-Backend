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
namespace Infrastructure.Data.Configurations.Identity;

internal sealed class PasswordResetRequestConfiguration : IEntityTypeConfiguration<PasswordResetRequest>
{
    public void Configure(EntityTypeBuilder<PasswordResetRequest> entity)
    {
        entity.ToTable("PasswordResetRequests");
        entity.HasIndex(x => x.TokenHash).IsUnique();
        entity.HasIndex(x => new { x.AccountId, x.RequestedAt });
        entity.Property(x => x.TokenHash).HasMaxLength(128);
        entity.HasOne(x => x.Account)
            .WithMany(x => x.PasswordResetRequests)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class AccountInvitationConfiguration : IEntityTypeConfiguration<AccountInvitation>
{
    public void Configure(EntityTypeBuilder<AccountInvitation> entity)
    {
        entity.ToTable("AccountInvitations");
        entity.HasIndex(x => x.TokenHash).IsUnique();
        entity.HasIndex(x => new { x.AccountId, x.InvitedAt });
        entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        entity.Property(x => x.Purpose).HasMaxLength(50).IsRequired();
        entity.HasOne(x => x.Account)
            .WithMany(x => x.AccountInvitations)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.ToTable("RefreshTokens");
        entity.HasIndex(x => x.TokenHash).IsUnique();
        entity.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.ReplacedByToken)
            .WithMany()
            .HasForeignKey(x => x.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
