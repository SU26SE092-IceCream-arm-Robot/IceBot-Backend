using Domain.Devices.ClientDevices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Devices;

internal sealed class ClientDeviceConfiguration : IEntityTypeConfiguration<ClientDevice>
{
    public void Configure(EntityTypeBuilder<ClientDevice> entity)
    {
        entity.ToTable("ClientDevices", table =>
        {
            table.HasCheckConstraint("CK_ClientDevices_PositiveVersions",
                "\"CredentialVersion\" > 0 AND \"SessionVersion\" > 0 AND \"Revision\" > 0");
        });
        entity.Property(device => device.DisplayName).HasMaxLength(200);
        entity.Property(device => device.AppVersion).HasMaxLength(100);
        entity.Property(device => device.Platform).HasMaxLength(100);
        entity.HasIndex(device => new { device.KioskId, device.Type })
            .IsUnique()
            .HasFilter("\"Type\" = 1 AND \"Status\" <> 3");
        entity.HasIndex(device => device.InstallationId)
            .IsUnique()
            .HasFilter("\"Status\" <> 3");
        entity.HasIndex(device => new { device.KioskId, device.Status });
        entity.HasOne(device => device.Organization).WithMany().HasForeignKey(device => device.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(device => device.Store).WithMany().HasForeignKey(device => device.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(device => device.Kiosk).WithMany().HasForeignKey(device => device.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(device => device.Credentials).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ClientDeviceCredentialConfiguration : IEntityTypeConfiguration<ClientDeviceCredential>
{
    public void Configure(EntityTypeBuilder<ClientDeviceCredential> entity)
    {
        entity.ToTable("ClientDeviceCredentials");
        entity.Property(credential => credential.SecretHash).HasColumnType("bytea");
        entity.Property(credential => credential.HashKeyVersion).HasMaxLength(100);
        entity.HasIndex(credential => new { credential.ClientDeviceId, credential.Version }).IsUnique();
        entity.HasIndex(credential => credential.ClientDeviceId).IsUnique().HasFilter("\"Status\" = 1");
        entity.HasOne(credential => credential.ClientDevice)
            .WithMany(device => device.Credentials)
            .HasForeignKey(credential => credential.ClientDeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ClientDeviceOperationReplayConfiguration : IEntityTypeConfiguration<ClientDeviceOperationReplay>
{
    public void Configure(EntityTypeBuilder<ClientDeviceOperationReplay> entity)
    {
        entity.ToTable("ClientDeviceOperationReplays");
        entity.Property(replay => replay.Operation).HasMaxLength(80);
        entity.Property(replay => replay.IdempotencyKey).HasMaxLength(200);
        entity.Property(replay => replay.RequestFingerprint).HasMaxLength(64);
        entity.HasIndex(replay => new { replay.KioskId, replay.Operation, replay.IdempotencyKey }).IsUnique();
        entity.HasIndex(replay => replay.ResultClientDeviceId);
        entity.HasOne<ClientDevice>().WithMany().HasForeignKey(replay => replay.ClientDeviceId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ClientDevice>().WithMany().HasForeignKey(replay => replay.ResultClientDeviceId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(replay => new { replay.ClientDeviceId, replay.Operation, replay.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"ClientDeviceId\" IS NOT NULL");
        entity.HasOne<Domain.Tenants.Entities.Kiosk>().WithMany().HasForeignKey(replay => replay.KioskId).OnDelete(DeleteBehavior.Restrict);
    }
}
