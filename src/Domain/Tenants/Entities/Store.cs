using Domain.Common;
using Domain.Common.Enums;

namespace Domain.Tenants.Entities;

public partial class Store : BusinessEntity
{
    public Guid OrganizationId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string StoreType { get; set; } = "Retail";

    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    public string? Country { get; set; }

    public string TimeZone { get; set; } = "Asia/Bangkok";

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public int OpeningHoursSchemaVersion { get; set; } = 1;

    public string? OpeningHoursJson { get; set; }

    public DateTimeOffset? SalesPausedAt { get; private set; }

    public DateTimeOffset? SalesPausedUntil { get; private set; }

    public string? SalesPauseReason { get; private set; }

    public Guid? SalesPausedByAccountId { get; private set; }

    public DateTimeOffset? SalesResumedAt { get; private set; }

    public Guid? SalesResumedByAccountId { get; private set; }

    public virtual ICollection<Kiosk> Kiosks { get; set; } = new List<Kiosk>();

    public virtual Organization Organization { get; set; } = null!;

    public bool IsSalesPausedAt(DateTimeOffset observedAt) =>
        SalesPausedAt.HasValue &&
        (!SalesResumedAt.HasValue || SalesResumedAt.Value < SalesPausedAt.Value) &&
        (!SalesPausedUntil.HasValue || observedAt < SalesPausedUntil.Value);

    public void PauseSales(
        DateTimeOffset pausedAt,
        Guid pausedByAccountId,
        string reason,
        DateTimeOffset? pausedUntil)
    {
        if (pausedByAccountId == Guid.Empty)
        {
            throw new DomainRuleException("Sales pause actor is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("Sales pause reason is required.");
        }

        if (pausedUntil.HasValue && pausedUntil.Value <= pausedAt)
        {
            throw new DomainRuleException("Sales pause end time must be in the future.");
        }

        SalesPausedAt = pausedAt;
        SalesPausedUntil = pausedUntil;
        SalesPauseReason = reason.Trim();
        SalesPausedByAccountId = pausedByAccountId;
        SalesResumedAt = null;
        SalesResumedByAccountId = null;
    }

    public void ResumeSales(DateTimeOffset resumedAt, Guid resumedByAccountId)
    {
        if (resumedByAccountId == Guid.Empty)
        {
            throw new DomainRuleException("Sales resume actor is required.");
        }

        if (!IsSalesPausedAt(resumedAt))
        {
            return;
        }

        SalesResumedAt = resumedAt;
        SalesResumedByAccountId = resumedByAccountId;
    }
}
