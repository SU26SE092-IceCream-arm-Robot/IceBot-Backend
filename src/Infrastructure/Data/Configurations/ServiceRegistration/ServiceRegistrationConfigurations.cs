using Domain.ContentManagement.Entities;
using Domain.ServiceRegistration.Entities;
using ServiceRegistrationEntity = Domain.ServiceRegistration.Entities.ServiceRegistration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.ServiceRegistration;

internal sealed class ServiceRegistrationConfiguration : IEntityTypeConfiguration<ServiceRegistrationEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRegistrationEntity> entity)
    {
        entity.ToTable("ServiceRegistrations");
        entity.Property(x => x.ReferenceCode).HasMaxLength(40).IsRequired();
        entity.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        entity.Property(x => x.RequestChecksum).HasMaxLength(64).IsRequired();
        entity.Property(x => x.ContactName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
        entity.Property(x => x.NormalizedEmail).HasMaxLength(320).IsRequired();
        entity.Property(x => x.PhoneNumber).HasMaxLength(50);
        entity.Property(x => x.NormalizedPhoneNumber).HasMaxLength(50);
        entity.Property(x => x.BusinessName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.LegalName).HasMaxLength(300);
        entity.Property(x => x.TaxCode).HasMaxLength(100);
        entity.Property(x => x.Address).HasMaxLength(500);
        entity.Property(x => x.Message).HasMaxLength(2_000);
        entity.Property(x => x.ReviewReason).HasMaxLength(1_000);
        entity.Property(x => x.ApprovedProvisioningJson).HasMaxLength(8_000);
        entity.Property(x => x.ProvisioningFailureCode).HasMaxLength(100);
        entity.Property(x => x.ProvisioningFailureMessage).HasMaxLength(1_000);
        entity.HasIndex(x => x.ReferenceCode).IsUnique();
        entity.HasIndex(x => x.IdempotencyKey).IsUnique();
        entity.HasIndex(x => new { x.Status, x.CreatedAt });
        entity.HasIndex(x => new { x.NormalizedEmail, x.CreatedAt });
        entity.HasIndex(x => x.ProvisionedOrganizationId).IsUnique().HasFilter("\"ProvisionedOrganizationId\" IS NOT NULL");
        entity.HasIndex(x => x.ProvisionedOrgAdminAccountId).IsUnique().HasFilter("\"ProvisionedOrgAdminAccountId\" IS NOT NULL");
    }
}

internal sealed class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> entity)
    {
        entity.ToTable("ContentPages");
        entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        entity.Property(x => x.DraftTitle).HasMaxLength(300).IsRequired();
        entity.Property(x => x.DraftBodyHtml).HasMaxLength(100_000).IsRequired();
        entity.HasIndex(x => x.Key).IsUnique();
        entity.HasIndex(x => x.Slug).IsUnique();
        entity.HasOne<ContentPageRevision>()
            .WithMany()
            .HasForeignKey(x => x.PublishedRevisionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ContentPageRevisionConfiguration : IEntityTypeConfiguration<ContentPageRevision>
{
    public void Configure(EntityTypeBuilder<ContentPageRevision> entity)
    {
        entity.ToTable("ContentPageRevisions");
        entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
        entity.Property(x => x.BodyHtml).HasMaxLength(100_000).IsRequired();
        entity.HasIndex(x => new { x.ContentPageId, x.RevisionNumber }).IsUnique();
        entity.HasOne<ContentPage>()
            .WithMany()
            .HasForeignKey(x => x.ContentPageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
