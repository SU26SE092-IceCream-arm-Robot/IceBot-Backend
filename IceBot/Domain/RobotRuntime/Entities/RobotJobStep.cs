using Domain.Common;
using Domain.RobotConfiguration.Entities;
using Domain.RobotRuntime.Enums;

namespace Domain.RobotRuntime.Entities;

public partial class RobotJobStep : RobotRuntimeAggregateEntity
{
    public Guid RobotJobId { get; set; }

    public Guid? RobotProgramStepId { get; set; }

    public int StepNumber { get; set; }

    public string StepCode { get; set; } = null!;

    public string Command { get; set; } = null!;

    public int ParametersSchemaVersion { get; set; } = 1;

    public string? ParametersJson { get; set; }

    public RobotJobStepStatus Status { get; set; } = RobotJobStepStatus.Pending;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? FailedAt { get; set; }

    public int? DurationMs { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetries { get; set; } = 3;

    public DateTimeOffset? NextRetryAt { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public virtual RobotJob RobotJob { get; set; } = null!;

    public virtual RobotProgramStep? RobotProgramStep { get; set; }

    public static RobotJobStep CreateFromProgramStep(RobotProgramStep programStep)
    {
        if (programStep == null)
        {
            throw new DomainRuleException("Program step is required.");
        }

        return new RobotJobStep
        {
            RobotProgramStepId = programStep.Id,
            StepNumber = programStep.StepNumber,
            StepCode = programStep.StepCode,
            Command = programStep.Command,
            ParametersSchemaVersion = programStep.ParametersOverrideJson is not null
                ? programStep.ParametersOverrideSchemaVersion
                : programStep.ParametersSchemaVersion,
            ParametersJson = programStep.ParametersOverrideJson ?? programStep.ParametersJson,
            MaxRetries = 3
        };
    }

    public void Start(DateTimeOffset startedAt)
    {
        if (Status is RobotJobStepStatus.Completed or RobotJobStepStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot start a completed or cancelled robot job step.");
        }

        Status = RobotJobStepStatus.Running;
        StartedAt ??= startedAt;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status == RobotJobStepStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot complete a cancelled robot job step.");
        }

        Status = RobotJobStepStatus.Completed;
        CompletedAt = completedAt;
        FailedAt = null;
        LastErrorCode = null;
        LastErrorMessage = null;
        SetDuration();
    }

    public void Fail(string errorCode, string errorMessage, DateTimeOffset failedAt)
    {
        if (Status == RobotJobStepStatus.Completed)
        {
            throw new DomainRuleException("Cannot fail a completed robot job step.");
        }

        Status = RobotJobStepStatus.Failed;
        FailedAt = failedAt;
        LastErrorCode = errorCode;
        LastErrorMessage = errorMessage;
        SetDuration();
    }

    public void ScheduleRetry(string errorCode, string errorMessage, DateTimeOffset nextRetryAt)
    {
        if (!CanRetry)
        {
            throw new DomainRuleException("Robot job step retry limit has been reached.");
        }

        RetryCount++;
        Status = RobotJobStepStatus.Pending;
        NextRetryAt = nextRetryAt;
        LastErrorCode = errorCode;
        LastErrorMessage = errorMessage;
    }

    public void Skip()
    {
        if (Status == RobotJobStepStatus.Completed)
        {
            throw new DomainRuleException("Cannot skip a completed robot job step.");
        }

        Status = RobotJobStepStatus.Skipped;
    }

    public void Cancel()
    {
        if (Status == RobotJobStepStatus.Completed)
        {
            throw new DomainRuleException("Cannot cancel a completed robot job step.");
        }

        Status = RobotJobStepStatus.Cancelled;
    }

    public bool CanRetry => RetryCount < MaxRetries;

    private void SetDuration()
    {
        if (StartedAt.HasValue && (CompletedAt.HasValue || FailedAt.HasValue))
        {
            var end = CompletedAt ?? FailedAt!.Value;
            DurationMs = (int)Math.Max(0, (end - StartedAt.Value).TotalMilliseconds);
        }
    }
}
