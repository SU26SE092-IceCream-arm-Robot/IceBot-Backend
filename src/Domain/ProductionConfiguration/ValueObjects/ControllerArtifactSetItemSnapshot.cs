using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
namespace Domain.ProductionConfiguration.ValueObjects;

public sealed record ControllerArtifactSetItemSnapshot(
    Guid ExecutionRouteId, Guid RobotProgramId, string RobotProgramManifestChecksum,
    Guid RobotArtifactId, string ArtifactChecksum, string StorageKey,
    string RuntimeTargetCode, string MachineModelCode, Guid? DeviceId,
    long ContentLengthBytes, int RunOrder, int ParametersSchemaVersion, string? ParametersJson);
