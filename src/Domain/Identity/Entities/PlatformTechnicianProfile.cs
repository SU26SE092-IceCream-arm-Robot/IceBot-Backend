using Domain.Common;

namespace Domain.Identity.Entities;

/// <summary>Marks an account as a platform-owned Technician.</summary>
public sealed class PlatformTechnicianProfile : BusinessEntity
{
    public Guid AccountId { get; set; }

    public Account Account { get; set; } = null!;
}
