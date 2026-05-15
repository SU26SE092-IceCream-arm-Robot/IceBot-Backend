using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public partial class RobotJob : RobotRuntimeAggregateEntity
{
    public Guid OrderId { get; set; }

    public Guid? OrderItemId { get; set; }

    public Guid KioskId { get; set; }

    public Guid? RobotProgramId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? RecipeId { get; set; }

    public string JobNumber { get; set; } = null!;

    public string? IdempotencyKey { get; set; }

    public Guid? ProductionRequestId { get; set; }

    public Guid? CorrelationId { get; set; }

    public RobotJobStatus Status { get; set; } = RobotJobStatus.Queued;

    public int Priority { get; set; }

    public string? ProductCode { get; set; }

    public int? RecipeVersion { get; set; }

    public int RecipeSnapshotSchemaVersion { get; set; } = 1;

    public string? RecipeSnapshotJson { get; set; }

    public DateTimeOffset RequestedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? FailedAt { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetries { get; set; } = 1;

    public DateTimeOffset? NextRetryAt { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public virtual Device? Device { get; set; }

    public virtual Kiosk Kiosk { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual OrderItem? OrderItem { get; set; }

    public virtual Recipe? Recipe { get; set; }

    public virtual ICollection<RobotJobStep> RobotJobSteps { get; set; } = new List<RobotJobStep>();

    public virtual RobotProgram? RobotProgram { get; set; }

    public RobotJobStep AddStepFromProgramStep(RobotProgramStep programStep)
    {
        if (Status is not (RobotJobStatus.Queued or RobotJobStatus.Paused))
        {
            throw new DomainRuleException("Robot job steps can only be added before execution starts.");
        }

        if (RobotJobSteps.Any(step => step.StepNumber == programStep.StepNumber))
        {
            throw new DomainRuleException("A robot job step with the same step number already exists.");
        }

        var jobStep = RobotJobStep.CreateFromProgramStep(programStep);
        RobotJobSteps.Add(jobStep);
        return jobStep;
    }

    public void Start(DateTimeOffset startedAt)
    {
        if (Status is RobotJobStatus.Completed or RobotJobStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot start a completed or cancelled robot job.");
        }

        if (!RobotJobSteps.Any())
        {
            throw new DomainRuleException("Cannot start a robot job without steps.");
        }

        Status = RobotJobStatus.Running;
        StartedAt ??= startedAt;
    }

    public void Pause()
    {
        if (Status != RobotJobStatus.Running)
        {
            throw new DomainRuleException("Only running robot jobs can be paused.");
        }

        Status = RobotJobStatus.Paused;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status == RobotJobStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot complete a cancelled robot job.");
        }

        if (RobotJobSteps.Any(step => step.Status == RobotJobStepStatus.Failed))
        {
            throw new DomainRuleException("Cannot complete a robot job with failed steps.");
        }

        Status = RobotJobStatus.Completed;
        CompletedAt = completedAt;
        FailedAt = null;
        FailureCode = null;
        FailureMessage = null;
        LastErrorCode = null;
        LastErrorMessage = null;
    }

    public void Fail(string failureCode, string failureMessage, DateTimeOffset failedAt)
    {
        if (Status == RobotJobStatus.Completed)
        {
            throw new DomainRuleException("Cannot fail a completed robot job.");
        }

        Status = RobotJobStatus.Failed;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        LastErrorCode = failureCode;
        LastErrorMessage = failureMessage;
        FailedAt = failedAt;
    }

    public void ScheduleRetry(string errorCode, string errorMessage, DateTimeOffset nextRetryAt)
    {
        if (!CanRetry)
        {
            throw new DomainRuleException("Robot job retry limit has been reached.");
        }

        RetryCount++;
        Status = RobotJobStatus.Queued;
        NextRetryAt = nextRetryAt;
        LastErrorCode = errorCode;
        LastErrorMessage = errorMessage;
    }

    public void Cancel(DateTimeOffset cancelledAt, string? reason = null)
    {
        if (Status == RobotJobStatus.Completed)
        {
            throw new DomainRuleException("Cannot cancel a completed robot job.");
        }

        Status = RobotJobStatus.Cancelled;
        FailedAt = cancelledAt;
        FailureMessage = reason ?? FailureMessage;

        foreach (var step in RobotJobSteps.Where(step => step.Status is RobotJobStepStatus.Pending or RobotJobStepStatus.Running))
        {
            step.Cancel();
        }
    }

    public RobotJobStep? GetNextPendingStep()
    {
        return RobotJobSteps
            .Where(step => step.Status == RobotJobStepStatus.Pending)
            .OrderBy(step => step.StepNumber)
            .FirstOrDefault();
    }

    public bool CanRetry => RetryCount < MaxRetries;
}
