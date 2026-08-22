using Domain.Common;
using Domain.Tenants.Entities;

namespace Domain.Identity.Entities;

public sealed class TechnicianSupportGrant : BusinessEntity
{
    public Guid AccountId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? StoreId { get; private set; }
    public Guid? KioskId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid? AssignedByAccountId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByAccountId { get; private set; }

    public Account Account { get; private set; } = null!;
    public Organization Organization { get; private set; } = null!;
    public Store? Store { get; private set; }
    public Kiosk? Kiosk { get; private set; }

    private TechnicianSupportGrant() { }

    public static TechnicianSupportGrant Create(
        Guid accountId, Guid organizationId, Guid? storeId, Guid? kioskId,
        DateTimeOffset assignedAt, Guid? actorId)
    {
        if (accountId == Guid.Empty || organizationId == Guid.Empty || storeId.HasValue == kioskId.HasValue)
            throw new DomainRuleException("A Technician support grant requires an account, organization, and exactly one store or kiosk.");

        return new TechnicianSupportGrant
        {
            AccountId = accountId,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            IsActive = true,
            AssignedAt = assignedAt,
            AssignedByAccountId = actorId,
            CreatedAt = assignedAt,
            CreatedByAccountId = actorId
        };
    }

    public void Revoke(DateTimeOffset revokedAt, Guid? actorId)
    {
        if (!IsActive) return;
        IsActive = false;
        RevokedAt = revokedAt;
        RevokedByAccountId = actorId;
        UpdatedAt = revokedAt;
        UpdatedByAccountId = actorId;
    }
}
