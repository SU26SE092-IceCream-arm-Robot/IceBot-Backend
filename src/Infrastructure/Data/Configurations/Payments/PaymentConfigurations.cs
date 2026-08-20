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
namespace Infrastructure.Data.Configurations.Payments;

internal sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> entity)
    {
        entity.ToTable("PaymentMethods");
        entity.HasIndex(x => x.Code).IsUnique();

    }
}

internal sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> entity)
    {
        entity.ToTable("PaymentTransactions");
        entity.HasIndex(x => x.TransactionNumber).IsUnique();
        entity.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
        entity.HasIndex(x => new { x.Provider, x.ProviderOrderCode })
            .IsUnique()
            .HasFilter("\"ProviderOrderCode\" IS NOT NULL");
        entity.HasIndex(x => x.ProviderTransactionId).HasFilter("\"ProviderTransactionId\" IS NOT NULL");
        entity.HasIndex(x => x.OrderId);
        entity.HasIndex(x => x.OrderId, "IX_PaymentTransactions_OrderId_PrimarySettlement")
            .IsUnique()
            .HasFilter("\"SettlementDisposition\" = 1");
        entity.Property(x => x.ProviderOrderCode).HasMaxLength(100);
        entity.Property(x => x.ProviderPaymentLinkId).HasMaxLength(200);
        entity.Property(x => x.ProviderStatus).HasMaxLength(100);
        entity.Property(x => x.CheckoutUrl).HasMaxLength(2048);
        entity.Property(x => x.QrCodePayload).HasMaxLength(2048);
        entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.PaymentMethod).WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class PaymentCallbackConfiguration : IEntityTypeConfiguration<PaymentCallback>
{
    public void Configure(EntityTypeBuilder<PaymentCallback> entity)
    {
        entity.ToTable("PaymentCallbacks");
        entity.HasIndex(x => new { x.Provider, x.ProviderEventId }).IsUnique().HasFilter("\"ProviderEventId\" IS NOT NULL");
        entity.HasOne(x => x.PaymentTransaction).WithMany().HasForeignKey(x => x.PaymentTransactionId).OnDelete(DeleteBehavior.Restrict);

    }
}

internal sealed class PaymentProviderObservationConfiguration : IEntityTypeConfiguration<PaymentProviderObservation>
{
    public void Configure(EntityTypeBuilder<PaymentProviderObservation> entity)
    {
        entity.ToTable("PaymentProviderObservations");
        entity.HasIndex(x => new { x.PaymentTransactionId, x.CloudReceivedAt });
        entity.HasIndex(x => new { x.Provider, x.ProviderOrderCode, x.CloudReceivedAt });
        entity.Property(x => x.Provider).HasMaxLength(50);
        entity.Property(x => x.ProviderOrderCode).HasMaxLength(100);
        entity.Property(x => x.ObservedStatus).HasMaxLength(100);
        entity.Property(x => x.ProviderTransactionId).HasMaxLength(200);
        entity.Property(x => x.FailureCode).HasMaxLength(100);
        entity.Property(x => x.FailureMessage).HasMaxLength(500);
        entity.HasOne(x => x.PaymentTransaction).WithMany().HasForeignKey(x => x.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> entity)
    {
        entity.ToTable("Refunds");
        entity.HasIndex(x => x.RefundNumber).IsUnique();
        entity.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
        entity.HasIndex(x => x.ProviderRefundId).HasFilter("\"ProviderRefundId\" IS NOT NULL");
        entity.Property(x => x.CompensationMethod).HasDefaultValue(Domain.Payments.Enums.RefundCompensationMethod.FullMoneyRefund);
        entity.HasOne(x => x.PaymentTransaction).WithMany().HasForeignKey(x => x.PaymentTransactionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.RequestedByAccount).WithMany().HasForeignKey(x => x.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);

    }
}
