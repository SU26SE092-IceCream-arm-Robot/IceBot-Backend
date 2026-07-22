namespace Domain.ProductionConfiguration.ValueObjects;

using Domain.RobotConfiguration.Programs;

public sealed record PublishedRobotProgramSnapshot(
    Guid Id,
    string Code,
    Guid OrganizationId,
    int ManifestSchemaVersion,
    string ManifestChecksum,
    IReadOnlyCollection<PublishedRobotArtifactSnapshot> Artifacts,
    RobotProgramRestartPolicy RestartPolicy = RobotProgramRestartPolicy.ManualOnly);

public sealed record PublishedRobotArtifactSnapshot(
    Guid ProgramArtifactId,
    Guid RobotArtifactId,
    int RunOrder,
    int ParametersSchemaVersion,
    string? ParametersJson,
    string Checksum,
    string StorageKey,
    string RuntimeTargetCode,
    string MachineModelCode,
    long ContentLengthBytes,
    Guid? TechnicalContractId = null,
    string? TechnicalContractChecksum = null,
    string? RequiredOptionCode = null);
