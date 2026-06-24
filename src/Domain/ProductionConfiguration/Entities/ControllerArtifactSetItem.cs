using Domain.Common;
using Domain.RobotConfiguration.Entities;

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

    internal static ControllerArtifactSetItem Create(
        Guid deploymentId,
        Guid routeId,
        RobotProgram program,
        RobotProgramArtifact programArtifact)
    {
        if (programArtifact.RobotArtifact is null ||
            string.IsNullOrWhiteSpace(program.ProgramManifestChecksum) ||
            string.IsNullOrWhiteSpace(programArtifact.RobotArtifact.Checksum))
        {
            throw new DomainRuleException("Active-set items require a published program manifest and artifact metadata.");
        }

        return new ControllerArtifactSetItem
        {
            ControllerArtifactSetDeploymentId = deploymentId,
            ExecutionRouteId = routeId,
            RobotProgramId = program.Id,
            RobotProgramManifestChecksum = program.ProgramManifestChecksum,
            RobotArtifactId = programArtifact.RobotArtifact.Id,
            ArtifactChecksum = programArtifact.RobotArtifact.Checksum,
            StorageKey = programArtifact.RobotArtifact.StorageKey,
            RuntimeTargetCode = programArtifact.RobotArtifact.RuntimeTargetCode,
            MachineModelCode = programArtifact.RobotArtifact.MachineModelCode,
            DeviceId = program.DeviceId,
            ContentLengthBytes = programArtifact.RobotArtifact.ContentLengthBytes,
            RunOrder = programArtifact.RunOrder,
            ParametersSchemaVersion = programArtifact.ParametersSchemaVersion,
            ParametersJson = programArtifact.ParametersJson
        };
    }
}
