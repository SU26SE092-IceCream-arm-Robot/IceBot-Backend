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
namespace Infrastructure.Data.Configurations.Sync;

internal sealed class SyncDeadLetterConfiguration : IEntityTypeConfiguration<SyncDeadLetter>
{
    public void Configure(EntityTypeBuilder<SyncDeadLetter> entity)
    {
        entity.ToTable("SyncDeadLetters");
        entity.HasIndex(x => new { x.Status, x.FailedAt });
        entity.HasIndex(x => x.EventId).HasFilter("\"EventId\" IS NOT NULL");
        entity.HasOne(x => x.SyncEventInbox).WithMany().HasForeignKey(x => x.SyncEventInboxId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.ResolvedByAccount).WithMany().HasForeignKey(x => x.ResolvedByAccountId).OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(x => x.RetryAttempts).UsePropertyAccessMode(PropertyAccessMode.Field);

    }
}

internal sealed class SyncDeadLetterRetryAttemptConfiguration : IEntityTypeConfiguration<SyncDeadLetterRetryAttempt>
{
    public void Configure(EntityTypeBuilder<SyncDeadLetterRetryAttempt> entity)
    {
        entity.ToTable("SyncDeadLetterRetryAttempts");
        entity.HasIndex(x => new { x.SyncDeadLetterId, x.AttemptNumber }).IsUnique();
        entity.Property(x => x.Reason).HasMaxLength(1000);
        entity.Property(x => x.ResultMessage).HasMaxLength(1000);
        entity.HasOne(x => x.SyncDeadLetter).WithMany(x => x.RetryAttempts).HasForeignKey(x => x.SyncDeadLetterId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.RequestedByAccount).WithMany().HasForeignKey(x => x.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);

    }
}
