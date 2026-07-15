using Domain.Devices.ExecutionEndpoints;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionConfiguration.Manifests;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.RobotConfiguration.Artifacts;

namespace Domain.ProductionConfiguration.Entities;

public class ControllerArtifactSetDeployment : AuditedEntity
{
    private readonly List<ControllerArtifactSetItem> _items = [];

    public Guid KioskId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid KioskExecutionEndpointId { get; private set; }
    public Guid ControllerId { get; private set; }
    public Guid SourceConfigurationReleaseId { get; private set; }
    public string ReleaseChecksum { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;
    public long ActiveSetVersion { get; private set; }
    public string ActiveSetChecksum { get; private set; } = null!;
    public int MaxArtifactCount { get; private set; }
    public long MaxArtifactStorageBytes { get; private set; }
    public int RequestedArtifactCount { get; private set; }
    public long RequestedArtifactStorageBytes { get; private set; }
    public ControllerArtifactSetDeploymentStatus Status { get; private set; } = ControllerArtifactSetDeploymentStatus.Pending;
    public Guid? RequestedByAccountId { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? ControllerReportedAt { get; private set; }
    public DateTimeOffset? CloudReceivedAt { get; private set; }
    public Guid? LastControllerReportId { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureReason { get; private set; }
    public string ValidationReportChecksum { get; private set; } = null!;
    public string RiskLevel { get; private set; } = null!;
    public string WarningCodesJson { get; private set; } = null!;
    public Guid? RiskAcknowledgedByAccountId { get; private set; }
    public DateTimeOffset? RiskAcknowledgedAt { get; private set; }

    public IReadOnlyCollection<ControllerArtifactSetItem> Items => _items;

    public virtual ConfigurationRelease SourceConfigurationRelease { get; private set; } = null!;
    public virtual KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    private ControllerArtifactSetDeployment()
    {
    }

    public static ControllerArtifactSetDeployment CreatePending(
        Guid kioskId, Guid organizationId, Guid endpointId, Guid controllerId, Guid releaseId, string releaseChecksum,
        long activeSetVersion,
        string idempotencyKey,
        int maxArtifactCount,
        long maxArtifactStorageBytes,
        Guid? requestedByAccountId,
        DateTimeOffset requestedAt,
        IEnumerable<ControllerArtifactSetItemSnapshot> items,
        string validationReportChecksum = "legacy",
        string riskLevel = "Legacy",
        string warningCodesJson = "[]",
        Guid? riskAcknowledgedByAccountId = null,
        DateTimeOffset? riskAcknowledgedAt = null)
    {
        if (kioskId == Guid.Empty || organizationId == Guid.Empty || endpointId == Guid.Empty || controllerId == Guid.Empty || releaseId == Guid.Empty ||
            string.IsNullOrWhiteSpace(releaseChecksum) ||
            activeSetVersion <= 0 || string.IsNullOrWhiteSpace(idempotencyKey) || maxArtifactCount <= 0 || maxArtifactStorageBytes <= 0)
        {
            throw new DomainRuleException("A published release, active low-cost endpoint, positive active-set version, and capacity limits are required.");
        }

        var deployment = new ControllerArtifactSetDeployment
        {
            KioskId = kioskId, OrganizationId = organizationId,
            KioskExecutionEndpointId = endpointId, ControllerId = controllerId,
            SourceConfigurationReleaseId = releaseId, ReleaseChecksum = releaseChecksum,
            ActiveSetVersion = activeSetVersion,
            IdempotencyKey = idempotencyKey.Trim(),
            MaxArtifactCount = maxArtifactCount,
            MaxArtifactStorageBytes = maxArtifactStorageBytes,
            RequestedByAccountId = requestedByAccountId,
            RequestedAt = requestedAt,
            ValidationReportChecksum = validationReportChecksum,
            RiskLevel = riskLevel,
            WarningCodesJson = warningCodesJson,
            RiskAcknowledgedByAccountId = riskAcknowledgedByAccountId ?? requestedByAccountId,
            RiskAcknowledgedAt = (riskAcknowledgedByAccountId ?? requestedByAccountId).HasValue
                ? riskAcknowledgedAt ?? requestedAt
                : null
        };

        var selected = items?.ToArray() ?? [];
        if (selected.Length == 0)
        {
            throw new DomainRuleException("A low-cost active artifact set requires at least one selected artifact.");
        }

        foreach (var item in selected) deployment._items.Add(ControllerArtifactSetItem.Create(deployment.Id, item));

        if (deployment._items.Select(item => item.RobotArtifactId).Distinct().Count() > maxArtifactCount ||
            deployment._items.GroupBy(item => item.RobotArtifactId).Sum(group => group.First().ContentLengthBytes) > maxArtifactStorageBytes)
        {
            throw new DomainRuleException("The requested active artifact set exceeds controller capacity.");
        }

        deployment.RequestedArtifactCount = deployment._items.Select(item => item.RobotArtifactId).Distinct().Count();
        deployment.RequestedArtifactStorageBytes = deployment._items.GroupBy(item => item.RobotArtifactId).Sum(group => group.First().ContentLengthBytes);
        deployment.ActiveSetChecksum = ControllerArtifactSetManifestBuilder.Create(deployment).Checksum;
        return deployment;
    }

    public bool MarkInstalled(Guid controllerReportId, DateTimeOffset controllerReportedAt, DateTimeOffset cloudReceivedAt)
    {
        if (IsDuplicateReport(controllerReportId)) return false;
        EnsureStatus(ControllerArtifactSetDeploymentStatus.Pending);
        ApplyReport(controllerReportId, controllerReportedAt, cloudReceivedAt);
        Status = ControllerArtifactSetDeploymentStatus.Installed;
        return true;
    }

    public bool MarkActive(Guid controllerReportId, DateTimeOffset controllerReportedAt, DateTimeOffset cloudReceivedAt)
    {
        if (IsDuplicateReport(controllerReportId)) return false;
        EnsureStatus(ControllerArtifactSetDeploymentStatus.Installed);
        ApplyReport(controllerReportId, controllerReportedAt, cloudReceivedAt);
        Status = ControllerArtifactSetDeploymentStatus.Active;
        return true;
    }

    public bool MarkFailed(Guid controllerReportId, DateTimeOffset controllerReportedAt, DateTimeOffset cloudReceivedAt, string failureCode, string? failureReason = null)
    {
        if (IsDuplicateReport(controllerReportId)) return false;
        if (Status is not (ControllerArtifactSetDeploymentStatus.Pending or ControllerArtifactSetDeploymentStatus.Installed) || string.IsNullOrWhiteSpace(failureCode))
        {
            throw new DomainRuleException("Only pending or installed active-set deployments can fail with a failure code.");
        }

        ApplyReport(controllerReportId, controllerReportedAt, cloudReceivedAt);
        FailureCode = failureCode.Trim();
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim();
        Status = ControllerArtifactSetDeploymentStatus.Failed;
        return true;
    }

    public void MarkCommandExpired(DateTimeOffset cloudObservedAt)
    {
        if (Status != ControllerArtifactSetDeploymentStatus.Pending)
        {
            throw new DomainRuleException("Only pending active-set deployments can expire before command acceptance.");
        }

        CloudReceivedAt = cloudObservedAt;
        FailureCode = "CommandExpired";
        FailureReason = "The deployment command expired before the execution endpoint accepted it.";
        Status = ControllerArtifactSetDeploymentStatus.Failed;
    }

    private bool IsDuplicateReport(Guid reportId) => reportId != Guid.Empty && LastControllerReportId == reportId;

    private void EnsureStatus(ControllerArtifactSetDeploymentStatus expected)
    {
        if (Status != expected) throw new DomainRuleException("Active-set deployment transition is not allowed.");
    }

    private void ApplyReport(Guid reportId, DateTimeOffset controllerReportedAt, DateTimeOffset cloudReceivedAt)
    {
        if (reportId == Guid.Empty) throw new DomainRuleException("Controller report id is required.");
        LastControllerReportId = reportId;
        ControllerReportedAt = controllerReportedAt;
        CloudReceivedAt = cloudReceivedAt;
    }
}
