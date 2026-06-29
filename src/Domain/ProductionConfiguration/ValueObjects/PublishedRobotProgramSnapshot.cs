namespace Domain.ProductionConfiguration.ValueObjects;

public sealed record PublishedRobotProgramSnapshot(
    Guid Id,
    string Code,
    Guid OrganizationId,
    int ManifestSchemaVersion,
    string ManifestChecksum,
    IReadOnlyCollection<PublishedRobotArtifactSnapshot> Artifacts);

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
    long ContentLengthBytes);
