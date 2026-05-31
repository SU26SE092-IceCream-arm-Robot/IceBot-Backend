using Domain.Common;

namespace Domain.RobotConfiguration.Entities;

public partial class RobotProgramStep : RobotConfigurationEntity
{
    public Guid RobotProgramId { get; set; }

    public Guid? TemplateStepId { get; set; }

    public int StepNumber { get; set; }

    public string StepCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string StepCommandType { get; set; } = null!;

    public string? TargetPointCode { get; set; }

    public string? VendorPointName { get; set; }

    public string? CoordinateSystem { get; set; }

    public string? ToolFrameCode { get; set; }

    public string? WorkpieceFrameCode { get; set; }

    public string? MotionProfileCode { get; set; }

    public int ParametersSchemaVersion { get; set; } = 1;

    public string? ParametersJson { get; set; }

    public int ParametersOverrideSchemaVersion { get; set; } = 1;

    public string? ParametersOverrideJson { get; set; }

    public decimal? SpeedScale { get; set; }

    public decimal? SafetyClearanceMm { get; set; }

    public int? ExpectedDurationMs { get; set; }

    public int RetryPolicySchemaVersion { get; set; } = 1;

    public string? RetryPolicyJson { get; set; }

    public bool IsRequired { get; set; } = true;

    public int? NextOnSuccessStepNumber { get; set; }

    public int? NextOnFailureStepNumber { get; set; }

    public int PointSnapshotSchemaVersion { get; set; } = 1;

    public string? PointSnapshotJson { get; set; }

    public virtual RobotProgramStep? TemplateStep { get; set; }

    public virtual RobotProgram RobotProgram { get; set; } = null!;

    public static RobotProgramStep Create(
        int stepNumber,
        string stepCode,
        string name,
        string stepCommandType,
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

        if (string.IsNullOrWhiteSpace(stepCommandType))
        {
            throw new DomainRuleException("Robot program step command type is required.");
        }

        return new RobotProgramStep
        {
            StepNumber = stepNumber,
            StepCode = stepCode.Trim(),
            Name = name.Trim(),
            StepCommandType = stepCommandType.Trim(),
            ParametersJson = parametersJson
        };
    }

    public void SetTargetPoint(
        string targetPointCode,
        string? vendorPointName = null,
        string? coordinateSystem = null,
        string? toolFrameCode = null,
        string? workpieceFrameCode = null,
        string? pointSnapshotJson = null)
    {
        if (string.IsNullOrWhiteSpace(targetPointCode))
        {
            throw new DomainRuleException("Target point code is required.");
        }

        TargetPointCode = targetPointCode.Trim();
        VendorPointName = string.IsNullOrWhiteSpace(vendorPointName) ? VendorPointName : vendorPointName.Trim();
        CoordinateSystem = string.IsNullOrWhiteSpace(coordinateSystem) ? CoordinateSystem : coordinateSystem.Trim();
        ToolFrameCode = string.IsNullOrWhiteSpace(toolFrameCode) ? ToolFrameCode : toolFrameCode.Trim();
        WorkpieceFrameCode = string.IsNullOrWhiteSpace(workpieceFrameCode) ? WorkpieceFrameCode : workpieceFrameCode.Trim();
        PointSnapshotJson = pointSnapshotJson ?? PointSnapshotJson;
    }

    public void SetMotionProfile(string motionProfileCode)
    {
        if (string.IsNullOrWhiteSpace(motionProfileCode))
        {
            throw new DomainRuleException("Motion profile code is required.");
        }

        MotionProfileCode = motionProfileCode.Trim();
    }
}
