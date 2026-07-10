using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Domain.Common;
using Domain.ProductionConfiguration.ValueObjects;

namespace Domain.ProductionConfiguration.Entities;

public class ControllerArtifactSetItem : AuditedEntity
{
    public Guid ControllerArtifactSetDeploymentId { get; private set; }
    public Guid ExecutionRouteId { get; private set; }
    public Guid RobotProgramId { get; private set; }
    public string RobotProgramManifestChecksum { get; private set; } = null!;
    public Guid RobotArtifactId { get; private set; }
    public string ArtifactChecksum { get; private set; } = null!;
    public string StorageKey { get; private set; } = null!;
    public string RuntimeTargetCode { get; private set; } = null!;
    public string MachineModelCode { get; private set; } = null!;
    public Guid? DeviceId { get; private set; }
    public long ContentLengthBytes { get; private set; }
    public int RunOrder { get; private set; }
    public int ParametersSchemaVersion { get; private set; }
    public string? ParametersJson { get; private set; }

    public virtual ControllerArtifactSetDeployment ControllerArtifactSetDeployment { get; private set; } = null!;

    private ControllerArtifactSetItem()
    {
    }

    internal static ControllerArtifactSetItem Create(Guid deploymentId, ControllerArtifactSetItemSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.RobotProgramManifestChecksum) || string.IsNullOrWhiteSpace(snapshot.ArtifactChecksum))
        {
            throw new DomainRuleException("Active-set items require a published program manifest and artifact metadata.");
        }

        return new ControllerArtifactSetItem
        {
            ControllerArtifactSetDeploymentId = deploymentId,
            ExecutionRouteId = snapshot.ExecutionRouteId, RobotProgramId = snapshot.RobotProgramId,
            RobotProgramManifestChecksum = snapshot.RobotProgramManifestChecksum,
            RobotArtifactId = snapshot.RobotArtifactId, ArtifactChecksum = snapshot.ArtifactChecksum,
            StorageKey = snapshot.StorageKey, RuntimeTargetCode = snapshot.RuntimeTargetCode,
            MachineModelCode = snapshot.MachineModelCode, DeviceId = snapshot.DeviceId,
            ContentLengthBytes = snapshot.ContentLengthBytes, RunOrder = snapshot.RunOrder,
            ParametersSchemaVersion = snapshot.ParametersSchemaVersion, ParametersJson = snapshot.ParametersJson
        };
    }
}
