using Domain.Common;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionConfiguration.Manifests;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.RobotConfiguration.Entities;

namespace Domain.ProductionConfiguration.Entities;

public class ControllerArtifactSetDeployment : AuditedEntity
{
    private readonly List<ControllerArtifactSetItem> _items = [];

    public Guid KioskId { get; private set; }
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

    public IReadOnlyCollection<ControllerArtifactSetItem> Items => _items;

    public virtual ConfigurationRelease SourceConfigurationRelease { get; private set; } = null!;
    public virtual KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    private ControllerArtifactSetDeployment()
    {
    }

    public static ControllerArtifactSetDeployment CreatePending(
        KioskExecutionEndpoint endpoint,
        ConfigurationRelease release,
        long activeSetVersion,
        string idempotencyKey,
        int maxArtifactCount,
        long maxArtifactStorageBytes,
        Guid? requestedByAccountId,
        DateTimeOffset requestedAt,
        IEnumerable<ControllerArtifactSetItemSelection> selections,
        bool isRollback = false)
    {
        if (endpoint is null || release is null || endpoint.ExecutionProfile != KioskExecutionProfile.LowCostController ||
            endpoint.Status != KioskExecutionEndpointStatus.Active || endpoint.ControllerId is null ||
            (release.Status != ConfigurationReleaseStatus.Published &&
                !(isRollback && release.Status == ConfigurationReleaseStatus.Retired)) ||
            string.IsNullOrWhiteSpace(release.ReleaseChecksum) ||
            activeSetVersion <= 0 || string.IsNullOrWhiteSpace(idempotencyKey) || maxArtifactCount <= 0 || maxArtifactStorageBytes <= 0)
        {
            throw new DomainRuleException("A published release, active low-cost endpoint, positive active-set version, and capacity limits are required.");
        }

        var deployment = new ControllerArtifactSetDeployment
        {
            KioskId = endpoint.KioskId,
            KioskExecutionEndpointId = endpoint.Id,
            ControllerId = endpoint.ControllerId.Value,
            SourceConfigurationReleaseId = release.Id,
            ReleaseChecksum = release.ReleaseChecksum,
            ActiveSetVersion = activeSetVersion,
            IdempotencyKey = idempotencyKey.Trim(),
            MaxArtifactCount = maxArtifactCount,
            MaxArtifactStorageBytes = maxArtifactStorageBytes,
            RequestedByAccountId = requestedByAccountId,
            RequestedAt = requestedAt
        };

        var selected = selections?.ToArray() ?? [];
        if (selected.Length == 0)
        {
            throw new DomainRuleException("A low-cost active artifact set requires at least one selected artifact.");
        }

        foreach (var selection in selected)
        {
            deployment._items.Add(ResolveItem(deployment.Id, endpoint, release, selection));
        }

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

    private static ControllerArtifactSetItem ResolveItem(Guid deploymentId, KioskExecutionEndpoint endpoint, ConfigurationRelease release, ControllerArtifactSetItemSelection selection)
    {
        var route = release.ExecutionRoutes.SingleOrDefault(candidate => candidate.Id == selection.ExecutionRouteId)
            ?? throw new DomainRuleException("Selected active-set route does not belong to the source release.");
        var binding = route.RobotBindings.SingleOrDefault(candidate => candidate.RobotProgramId == selection.RobotProgramId)
            ?? throw new DomainRuleException("Selected active-set program does not belong to the source route.");
        var program = binding.RobotProgram ?? throw new DomainRuleException("Robot program must be loaded before active-set materialization.");
        var programArtifact = program.RobotProgramArtifacts.SingleOrDefault(candidate => candidate.RobotArtifactId == selection.RobotArtifactId && candidate.RunOrder == selection.RunOrder)
            ?? throw new DomainRuleException("Selected active-set artifact does not belong to the selected robot program.");

        if (programArtifact.RobotArtifact is null || !endpoint.SupportsRobotTarget(programArtifact.RobotArtifact.RuntimeTargetCode, programArtifact.RobotArtifact.MachineModelCode, program.DeviceId))
        {
            throw new DomainRuleException("Selected active-set artifact is not compatible with the controller endpoint.");
        }

        return ControllerArtifactSetItem.Create(deploymentId, route.Id, program, programArtifact);
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
