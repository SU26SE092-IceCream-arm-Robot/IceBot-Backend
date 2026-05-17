using Domain.Common;
using Domain.Identity.Entities;
using Domain.RobotConfiguration.Enums;

namespace Domain.RobotConfiguration.Entities;

public partial class RobotProgramStep : RobotConfigurationEntity
{
    public Guid RobotProgramId { get; set; }

    public Guid? TemplateStepId { get; set; }

    public Guid? CalibratedByAccountId { get; set; }

    public int StepNumber { get; set; }

    public string StepCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Command { get; set; } = null!;

    public int ParametersSchemaVersion { get; set; } = 1;

    public string? ParametersJson { get; set; }

    public int ParametersOverrideSchemaVersion { get; set; } = 1;

    public string? ParametersOverrideJson { get; set; }

    public bool RequiresCalibration { get; set; }

    public string? CalibrationKey { get; set; }

    public CalibrationStatus CalibrationStatus { get; set; } = CalibrationStatus.NotRequired;

    public string? CoordinateFrame { get; set; }

    public string? ToolName { get; set; }

    public decimal? TargetX { get; set; }

    public decimal? TargetY { get; set; }

    public decimal? TargetZ { get; set; }

    public decimal? TargetRx { get; set; }

    public decimal? TargetRy { get; set; }

    public decimal? TargetRz { get; set; }

    public decimal? OffsetX { get; set; }

    public decimal? OffsetY { get; set; }

    public decimal? OffsetZ { get; set; }

    public decimal? SpeedScale { get; set; }

    public decimal? SafetyClearanceMm { get; set; }

    public int? ExpectedDurationMs { get; set; }

    public int RetryPolicySchemaVersion { get; set; } = 1;

    public string? RetryPolicyJson { get; set; }

    public bool IsRequired { get; set; } = true;

    public int? NextOnSuccessStepNumber { get; set; }

    public int? NextOnFailureStepNumber { get; set; }

    public int CalibrationDataSchemaVersion { get; set; } = 1;

    public string? CalibrationDataJson { get; set; }

    public int ValidationResultSchemaVersion { get; set; } = 1;

    public string? ValidationResultJson { get; set; }

    public DateTimeOffset? CalibratedAt { get; set; }

    public DateTimeOffset? ValidatedAt { get; set; }

    public virtual RobotProgramStep? TemplateStep { get; set; }

    public virtual Account? CalibratedByAccount { get; set; }

    public virtual RobotProgram RobotProgram { get; set; } = null!;

    public static RobotProgramStep Create(
        int stepNumber,
        string stepCode,
        string name,
        string command,
        string? parametersJson = null)
    {
        if (stepNumber <= 0)
        {
            throw new DomainRuleException("Robot program step number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(stepCode))
        {
            throw new DomainRuleException("Robot program step code is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Robot program step name is required.");
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new DomainRuleException("Robot program step command is required.");
        }

        return new RobotProgramStep
        {
            StepNumber = stepNumber,
            StepCode = stepCode.Trim(),
            Name = name.Trim(),
            Command = command.Trim(),
            ParametersJson = parametersJson
        };
    }

    public void RequireCalibration(string calibrationKey)
    {
        if (string.IsNullOrWhiteSpace(calibrationKey))
        {
            throw new DomainRuleException("Calibration key is required.");
        }

        RequiresCalibration = true;
        CalibrationKey = calibrationKey.Trim();
        CalibrationStatus = CalibrationStatus.Pending;
    }

    public void ApplyCalibration(
        decimal? targetX,
        decimal? targetY,
        decimal? targetZ,
        decimal? targetRx,
        decimal? targetRy,
        decimal? targetRz,
        DateTimeOffset calibratedAt,
        Guid? calibratedByAccountId,
        string? parametersOverrideJson = null)
    {
        if (!RequiresCalibration)
        {
            throw new DomainRuleException("This robot program step does not require calibration.");
        }

        TargetX = targetX;
        TargetY = targetY;
        TargetZ = targetZ;
        TargetRx = targetRx;
        TargetRy = targetRy;
        TargetRz = targetRz;
        ParametersOverrideJson = parametersOverrideJson ?? ParametersOverrideJson;
        CalibratedAt = calibratedAt;
        CalibratedByAccountId = calibratedByAccountId;
        CalibrationStatus = CalibrationStatus.Calibrated;
    }

    public void MarkCalibrationValidated(DateTimeOffset validatedAt, string? validationResultJson = null)
    {
        if (RequiresCalibration && CalibrationStatus != CalibrationStatus.Calibrated)
        {
            throw new DomainRuleException("Only calibrated steps can be validated.");
        }

        ValidationResultJson = validationResultJson ?? ValidationResultJson;
        ValidatedAt = validatedAt;
        CalibrationStatus = CalibrationStatus.Validated;
    }
}
