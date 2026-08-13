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

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> entity)
    {
        entity.ToTable("Accounts");
        entity.HasIndex(x => x.UserName).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => x.Email).IsUnique().HasFilter(EfModelConfigurationConstants.ActiveRowFilter);
        entity.HasIndex(x => x.GoogleSubjectId).IsUnique().HasFilter(EfModelConfigurationConstants.NotNullAndActive(nameof(Account.GoogleSubjectId)));
        entity.HasIndex(x => x.GoogleEmail).HasFilter("\"GoogleEmail\" IS NOT NULL");
        entity.Property(x => x.WorkforceRevision).IsConcurrencyToken();

        entity.Property(x => x.Password)
            .HasConversion(
                password => password == null ? null : password.Value,
                hash => string.IsNullOrWhiteSpace(hash) ? null : HashedPassword.From(hash))
            .HasColumnName("PasswordHash")
            .HasMaxLength(512);

        entity.HasMany(x => x.AccountRoles)
            .WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(x => x.Stores)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "AccountStores",
                right => right.HasOne<Store>().WithMany().HasForeignKey("StoreId").OnDelete(DeleteBehavior.Restrict),
                left => left.HasOne<Account>().WithMany().HasForeignKey("AccountId").OnDelete(DeleteBehavior.Restrict),
                join =>
                {
                    join.ToTable("AccountStores");
                    join.HasKey("AccountId", "StoreId");
                });

    }
}

internal sealed class StaffWorkforceLifecycleTransitionConfiguration : IEntityTypeConfiguration<StaffWorkforceLifecycleTransition>
{
    public void Configure(EntityTypeBuilder<StaffWorkforceLifecycleTransition> entity)
    {
        entity.ToTable("StaffWorkforceLifecycleTransitions");
        entity.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        entity.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired();
        entity.Property(x => x.RequestIdempotencyKey).HasMaxLength(128);
        entity.HasIndex(x => new { x.AccountId, x.CreatedAt });
        entity.HasIndex(x => new { x.OrganizationId, x.RequestIdempotencyKey }).IsUnique().HasFilter("\"RequestIdempotencyKey\" IS NOT NULL");
    }
}

internal sealed class StaffWorkforceCreateReplayConfiguration : IEntityTypeConfiguration<StaffWorkforceCreateReplay>
{
    public void Configure(EntityTypeBuilder<StaffWorkforceCreateReplay> entity)
    {
        entity.ToTable("StaffWorkforceCreateReplays");
        entity.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        entity.Property(x => x.RequestFingerprint).HasMaxLength(64).IsRequired();
        entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique();
    }
}

internal sealed class AccountRoleConfiguration : IEntityTypeConfiguration<AccountRole>
{
    public void Configure(EntityTypeBuilder<AccountRole> entity)
    {
        entity.ToTable("AccountRoles");
        entity.HasIndex(x => new { x.AccountId, x.RoleId, x.OrganizationId, x.StoreId, x.KioskId }).IsUnique();
        entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId });
        entity.HasOne(x => x.Role)
            .WithMany(x => x.AccountRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk)
            .WithMany()
            .HasForeignKey(x => x.KioskId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.AssignedByAccount)
            .WithMany()
            .HasForeignKey(x => x.AssignedByAccountId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> entity)
    {
        entity.ToTable("Roles");
        entity.HasIndex(x => x.Code).IsUnique();

    }
}

internal sealed class AccountNotificationDeviceConfiguration : IEntityTypeConfiguration<AccountNotificationDevice>
{
    public void Configure(EntityTypeBuilder<AccountNotificationDevice> entity)
    {
        entity.ToTable("AccountNotificationDevices");
        entity.HasIndex(x => new { x.AccountId, x.InstallationId }).IsUnique();
        entity.HasIndex(x => x.PushTokenHash)
            .IsUnique()
            .HasFilter("\"PushTokenHash\" IS NOT NULL AND \"InvalidatedAt\" IS NULL");
        entity.HasIndex(x => new { x.AccountId, x.InvalidatedAt });
        entity.Property(x => x.Platform).HasMaxLength(500).IsRequired();
        entity.Property(x => x.PushToken).HasMaxLength(4_096);
        entity.Property(x => x.PushTokenHash).HasMaxLength(500);
        entity.Property(x => x.DeviceName).HasMaxLength(500);
        entity.Property(x => x.AppVersion).HasMaxLength(500);
        entity.Property(x => x.InvalidationReason).HasMaxLength(256);
        entity.HasOne(x => x.Account)
            .WithMany(x => x.NotificationDevices)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
