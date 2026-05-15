using Domain.Common;
using Domain.Enums;
using Domain.Identity.Entities;

namespace Domain.Entities;

public partial class RobotProgram : RobotConfigurationEntity, IKioskScoped
{
    public Guid? OrganizationId { get; set; }

    public Guid? StoreId { get; set; }

    public Guid? KioskId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? TemplateProgramId { get; set; }

    public Guid? CalibratedByAccountId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public TenantScopeType ScopeType { get; set; } = TenantScopeType.Global;

    public string ProductType { get; set; } = "IceCream";

    public int ProgramVersion { get; set; } = 1;

    public RobotProgramStatus Status { get; set; } = RobotProgramStatus.Draft;

    public string Vendor { get; set; } = "Farino";

    public string? VendorProgramId { get; set; }

    public string? VendorProgramVersion { get; set; }

    public long? SupportedDeviceTypeId { get; set; }

    public int? EstimatedDurationSeconds { get; set; }

    public bool IsDefault { get; set; }

    public CalibrationStatus CalibrationStatus { get; set; } = CalibrationStatus.NotRequired;

    public string? Description { get; set; }

    public int ProgramPayloadSchemaVersion { get; set; } = 1;

    public string? ProgramPayloadJson { get; set; }

    public int CalibrationDataSchemaVersion { get; set; } = 1;

    public string? CalibrationDataJson { get; set; }

    public int SafetyZoneSchemaVersion { get; set; } = 1;

    public string? SafetyZoneJson { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset? CalibratedAt { get; set; }

    public DateTimeOffset? ActivatedAt { get; set; }

    public DateTimeOffset? RetiredAt { get; set; }

    public virtual Organization? Organization { get; set; }

    public virtual Store? Store { get; set; }

    public virtual Kiosk? Kiosk { get; set; }

    public virtual Device? Device { get; set; }

    public virtual RobotProgram? TemplateProgram { get; set; }

    public virtual Account? CalibratedByAccount { get; set; }

    public virtual ICollection<RobotProgramStep> RobotProgramSteps { get; set; } = new List<RobotProgramStep>();

    public RobotProgramStep AddStep(
        int stepNumber,
        string stepCode,
        string name,
        string command,
        string? parametersJson = null)
    {
        EnsureDraft();

        if (RobotProgramSteps.Any(step => step.StepNumber == stepNumber))
        {
            throw new DomainRuleException("A robot program step with the same step number already exists.");
        }

        if (RobotProgramSteps.Any(step => string.Equals(step.StepCode, stepCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainRuleException("A robot program step with the same code already exists.");
        }

        var step = RobotProgramStep.Create(stepNumber, stepCode, name, command, parametersJson);
        RobotProgramSteps.Add(step);
        return step;
    }

    public void Publish(DateTimeOffset publishedAt)
    {
        EnsureDraft();

        if (!RobotProgramSteps.Any())
        {
            throw new DomainRuleException("Cannot publish a robot program without steps.");
        }

        Status = RobotProgramStatus.Published;
        PublishedAt = publishedAt;
    }

    public void Activate(DateTimeOffset activatedAt)
    {
        if (Status is not (RobotProgramStatus.Published or RobotProgramStatus.Active))
        {
            throw new DomainRuleException("Only published robot programs can be activated.");
        }

        if (RobotProgramSteps.Any(step => step.RequiresCalibration && step.CalibrationStatus != CalibrationStatus.Validated))
        {
            throw new DomainRuleException("Cannot activate a robot program with unvalidated calibration.");
        }

        Status = RobotProgramStatus.Active;
        ActivatedAt = activatedAt;
    }

    public void Retire(DateTimeOffset retiredAt)
    {
        if (Status == RobotProgramStatus.Draft)
        {
            throw new DomainRuleException("Draft robot programs should be deleted or disabled, not retired.");
        }

        Status = RobotProgramStatus.Retired;
        RetiredAt = retiredAt;
    }

    public void MarkCalibrationPending()
    {
        CalibrationStatus = CalibrationStatus.Pending;
    }

    public void MarkCalibrationValidated(DateTimeOffset calibratedAt, Guid? calibratedByAccountId)
    {
        if (RobotProgramSteps.Any(step => step.RequiresCalibration && step.CalibrationStatus != CalibrationStatus.Validated))
        {
            throw new DomainRuleException("All calibration-required steps must be validated first.");
        }

        CalibrationStatus = CalibrationStatus.Validated;
        CalibratedAt = calibratedAt;
        CalibratedByAccountId = calibratedByAccountId;
    }

    private void EnsureDraft()
    {
        if (Status != RobotProgramStatus.Draft)
        {
            throw new DomainRuleException("Only draft robot programs can be modified.");
        }
    }
}
