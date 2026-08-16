using Domain.Devices.ExecutionEndpoints;
using Domain.Common;
using Domain.ProductionConfiguration.Enums;

namespace Domain.ProductionConfiguration.Entities;

public class KioskConfigurationDeployment : BusinessEntity
{
    public Guid KioskId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid KioskExecutionEndpointId { get; private set; }

    public Guid EdgeRuntimeId { get; private set; }

    public Guid ConfigurationReleaseId { get; private set; }

    public string ReleaseChecksum { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;

    public int AttemptNo { get; private set; }

    public KioskConfigurationDeploymentStatus Status { get; private set; } = KioskConfigurationDeploymentStatus.Pending;

    public DateTimeOffset RequestedAt { get; private set; }

    public Guid? RequestedByAccountId { get; private set; }

    public DateTimeOffset? EdgeReportedAt { get; private set; }

    public DateTimeOffset? CloudReceivedAt { get; private set; }

    public Guid? LastEdgeDeploymentEventId { get; private set; }

    public string? FailureCode { get; private set; }

    public string? FailureReason { get; private set; }

    public string ValidationReportChecksum { get; private set; } = null!;
    public string RiskLevel { get; private set; } = null!;
    public string WarningCodesJson { get; private set; } = null!;
    public Guid? RiskAcknowledgedByAccountId { get; private set; }
    public DateTimeOffset? RiskAcknowledgedAt { get; private set; }

    public virtual ConfigurationRelease ConfigurationRelease { get; private set; } = null!;

    public virtual Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    private KioskConfigurationDeployment()
    {
    }

    public static KioskConfigurationDeployment CreatePending(
        Guid kioskId,
        Guid organizationId,
        Guid endpointId,
        Guid edgeRuntimeId,
        Guid configurationReleaseId,
        string releaseChecksum,
        int attemptNo,
        string idempotencyKey,
        DateTimeOffset requestedAt,
        Guid? requestedByAccountId,
        string validationReportChecksum,
        string riskLevel,
        string warningCodesJson,
        Guid? riskAcknowledgedByAccountId = null,
        DateTimeOffset? riskAcknowledgedAt = null)
    {
        if (kioskId == Guid.Empty || organizationId == Guid.Empty || endpointId == Guid.Empty || edgeRuntimeId == Guid.Empty ||
            configurationReleaseId == Guid.Empty || string.IsNullOrWhiteSpace(releaseChecksum))
        {
            throw new DomainRuleException("An active Full Edge endpoint and a published configuration release checksum are required.");
        }

        if (attemptNo <= 0 || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new DomainRuleException("Configuration deployment attempt number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(validationReportChecksum) ||
            string.IsNullOrWhiteSpace(riskLevel) ||
            string.IsNullOrWhiteSpace(warningCodesJson) ||
            string.Equals(validationReportChecksum, "legacy", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(riskLevel, "legacy", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainRuleException("Configuration deployment requires non-legacy validation evidence.");
        }

        return new KioskConfigurationDeployment
        {
            KioskId = kioskId,
            OrganizationId = organizationId,
            KioskExecutionEndpointId = endpointId,
            EdgeRuntimeId = edgeRuntimeId,
            ConfigurationReleaseId = configurationReleaseId,
            ReleaseChecksum = releaseChecksum,
            IdempotencyKey = idempotencyKey.Trim(),
            AttemptNo = attemptNo,
            RequestedAt = requestedAt,
            RequestedByAccountId = requestedByAccountId,
            ValidationReportChecksum = validationReportChecksum,
            RiskLevel = riskLevel,
            WarningCodesJson = warningCodesJson,
            RiskAcknowledgedByAccountId = riskAcknowledgedByAccountId ?? requestedByAccountId,
            RiskAcknowledgedAt = (riskAcknowledgedByAccountId ?? requestedByAccountId).HasValue
                ? riskAcknowledgedAt ?? requestedAt
                : null
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

    public void MarkCommandExpired(DateTimeOffset cloudObservedAt)
    {
        if (Status != KioskConfigurationDeploymentStatus.Pending)
        {
            throw new DomainRuleException("Only pending configuration deployments can expire before command acceptance.");
        }

        CloudReceivedAt = cloudObservedAt;
        FailureCode = "CommandExpired";
        FailureReason = "The deployment command expired before the execution endpoint accepted it.";
        Status = KioskConfigurationDeploymentStatus.Failed;
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
