using Domain.Common;
using Domain.ProductionConfiguration.Enums;

namespace Domain.ProductionConfiguration.Entities;

public class KioskConfigurationDeployment : BusinessEntity
{
    public Guid KioskId { get; private set; }

    public Guid KioskExecutionEndpointId { get; private set; }

    public Guid EdgeRuntimeId { get; private set; }

    public Guid ConfigurationReleaseId { get; private set; }

    public string ReleaseChecksum { get; private set; } = null!;

    public int AttemptNo { get; private set; }

    public KioskConfigurationDeploymentStatus Status { get; private set; } = KioskConfigurationDeploymentStatus.Pending;

    public DateTimeOffset RequestedAt { get; private set; }

    public Guid? RequestedByAccountId { get; private set; }

    public DateTimeOffset? EdgeReportedAt { get; private set; }

    public DateTimeOffset? CloudReceivedAt { get; private set; }

    public Guid? LastEdgeDeploymentEventId { get; private set; }

    public string? FailureCode { get; private set; }

    public string? FailureReason { get; private set; }

    public virtual ConfigurationRelease ConfigurationRelease { get; private set; } = null!;

    public virtual Domain.Devices.Entities.KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    private KioskConfigurationDeployment()
    {
    }

    public static KioskConfigurationDeployment CreatePending(
        Domain.Devices.Entities.KioskExecutionEndpoint kioskExecutionEndpoint,
        ConfigurationRelease configurationRelease,
        int attemptNo,
        DateTimeOffset requestedAt,
        Guid? requestedByAccountId = null)
    {
        if (kioskExecutionEndpoint is null || configurationRelease is null ||
            kioskExecutionEndpoint.FullEdgeRuntimeId is null ||
            string.IsNullOrWhiteSpace(configurationRelease.ReleaseChecksum))
        {
            throw new DomainRuleException("An active Full Edge endpoint and a published configuration release checksum are required.");
        }

        if (attemptNo <= 0)
        {
            throw new DomainRuleException("Configuration deployment attempt number must be greater than zero.");
        }

        configurationRelease.ValidateFullEdgeDeploymentTarget(
            kioskExecutionEndpoint,
            kioskExecutionEndpoint.FullEdgeRuntimeId.Value);

        return new KioskConfigurationDeployment
        {
            KioskId = kioskExecutionEndpoint.KioskId,
            KioskExecutionEndpointId = kioskExecutionEndpoint.Id,
            EdgeRuntimeId = kioskExecutionEndpoint.FullEdgeRuntimeId.Value,
            ConfigurationReleaseId = configurationRelease.Id,
            ReleaseChecksum = configurationRelease.ReleaseChecksum,
            AttemptNo = attemptNo,
            RequestedAt = requestedAt,
            RequestedByAccountId = requestedByAccountId
        };
    }

    public bool MarkInstalled(Guid edgeDeploymentEventId, DateTimeOffset edgeReportedAt, DateTimeOffset cloudReceivedAt)
    {
        if (IsDuplicateAcknowledgement(edgeDeploymentEventId))
        {
            return false;
        }

        EnsurePending();
        ApplyEdgeAcknowledgement(edgeDeploymentEventId, edgeReportedAt, cloudReceivedAt);
        Status = KioskConfigurationDeploymentStatus.Installed;
        return true;
    }

    public bool MarkActive(Guid edgeDeploymentEventId, DateTimeOffset edgeReportedAt, DateTimeOffset cloudReceivedAt)
    {
        if (IsDuplicateAcknowledgement(edgeDeploymentEventId))
        {
            return false;
        }

        if (Status != KioskConfigurationDeploymentStatus.Installed)
        {
            throw new DomainRuleException("Only installed configuration deployments can become active.");
        }

        ApplyEdgeAcknowledgement(edgeDeploymentEventId, edgeReportedAt, cloudReceivedAt);
        Status = KioskConfigurationDeploymentStatus.Active;
        return true;
    }

    public bool MarkFailed(
        Guid edgeDeploymentEventId,
        DateTimeOffset edgeReportedAt,
        DateTimeOffset cloudReceivedAt,
        string failureCode,
        string? failureReason = null)
    {
        if (IsDuplicateAcknowledgement(edgeDeploymentEventId))
        {
            return false;
        }

        if (Status is not (KioskConfigurationDeploymentStatus.Pending or KioskConfigurationDeploymentStatus.Installed))
        {
            throw new DomainRuleException("Only pending or installed configuration deployments can fail.");
        }

        if (string.IsNullOrWhiteSpace(failureCode))
        {
            throw new DomainRuleException("Configuration deployment failure code is required.");
        }

        ApplyEdgeAcknowledgement(edgeDeploymentEventId, edgeReportedAt, cloudReceivedAt);
        FailureCode = failureCode.Trim();
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim();
        Status = KioskConfigurationDeploymentStatus.Failed;
        return true;
    }

    private void EnsurePending()
    {
        if (Status != KioskConfigurationDeploymentStatus.Pending)
        {
            throw new DomainRuleException("Only pending configuration deployments can be marked installed.");
        }
    }

    private void ApplyEdgeAcknowledgement(Guid edgeDeploymentEventId, DateTimeOffset edgeReportedAt, DateTimeOffset cloudReceivedAt)
    {
        if (edgeDeploymentEventId == Guid.Empty)
        {
            throw new DomainRuleException("Edge deployment event id is required.");
        }

        LastEdgeDeploymentEventId = edgeDeploymentEventId;
        EdgeReportedAt = edgeReportedAt;
        CloudReceivedAt = cloudReceivedAt;
    }

    private bool IsDuplicateAcknowledgement(Guid edgeDeploymentEventId)
    {
        return edgeDeploymentEventId != Guid.Empty && LastEdgeDeploymentEventId == edgeDeploymentEventId;
    }
}
