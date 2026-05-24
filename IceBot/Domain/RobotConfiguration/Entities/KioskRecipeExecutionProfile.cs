using Domain.Catalog.Entities;
using Domain.Common;
using Domain.Devices.Entities;
using Domain.RobotConfiguration.Enums;
using Domain.Tenants.Entities;

namespace Domain.RobotConfiguration.Entities;

public partial class KioskRecipeExecutionProfile : RobotConfigurationEntity, IKioskScoped
{
    public Guid? OrganizationId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid RecipeId { get; set; }

    public Guid RobotProgramId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public KioskRecipeExecutionProfileStatus Status { get; set; } = KioskRecipeExecutionProfileStatus.Draft;

    public int Priority { get; set; }

    public bool IsDefault { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset? ActivatedAt { get; set; }

    public DateTimeOffset? RetiredAt { get; set; }

    public int ResolverPolicySchemaVersion { get; set; } = 1;

    public string? ResolverPolicyJson { get; set; }

    public int ExecutionSnapshotSchemaVersion { get; set; } = 1;

    public string? ExecutionSnapshotJson { get; set; }

    public string? Notes { get; set; }

    public virtual Organization? Organization { get; set; }

    public virtual Store? Store { get; set; }

    public virtual Kiosk? Kiosk { get; set; }

    public virtual Device? Device { get; set; }

    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;

    public virtual RobotProgram RobotProgram { get; set; } = null!;

    public void Activate(DateTimeOffset activatedAt)
    {
        if (Status is not (KioskRecipeExecutionProfileStatus.Draft or KioskRecipeExecutionProfileStatus.Active))
        {
            throw new DomainRuleException("Only draft or active execution profiles can be activated.");
        }

        Status = KioskRecipeExecutionProfileStatus.Active;
        ActivatedAt = activatedAt;
    }

    public void Disable()
    {
        if (Status == KioskRecipeExecutionProfileStatus.Retired)
        {
            throw new DomainRuleException("Cannot disable a retired execution profile.");
        }

        Status = KioskRecipeExecutionProfileStatus.Disabled;
    }

    public void Retire(DateTimeOffset retiredAt)
    {
        Status = KioskRecipeExecutionProfileStatus.Retired;
        RetiredAt = retiredAt;
    }
}
