using Domain.RobotConfiguration.Entities;
using Domain.RobotConfiguration.Manifests;

namespace Application.RobotConfiguration.Results;

public sealed class RobotProgramResult
{
    public Guid Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string ScopeType { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string? Description { get; init; }
    public int ProgramManifestSchemaVersion { get; init; }
    public string? ProgramManifestJson { get; init; }
    public string? ProgramManifestChecksum { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public IReadOnlyCollection<RobotProgramArtifactResult> Artifacts { get; init; } = Array.Empty<RobotProgramArtifactResult>();

    public static RobotProgramResult FromEntity(
        RobotProgram program,
        IReadOnlyCollection<RobotArtifactManifestSnapshot>? artifactSnapshots = null)
    {
        var snapshotsById = artifactSnapshots?.ToDictionary(item => item.RobotArtifactId)
            ?? new Dictionary<Guid, RobotArtifactManifestSnapshot>();
        return new RobotProgramResult
        {
            Id = program.Id,
            OrganizationId = program.OrganizationId,
            StoreId = program.StoreId,
            KioskId = program.KioskId,
            DeviceId = program.DeviceId,
            Code = program.Code,
            Name = program.Name,
            ScopeType = program.ScopeType.ToString(),
            Status = program.Status.ToString(),
            Description = program.Description,
            ProgramManifestSchemaVersion = program.ProgramManifestSchemaVersion,
            ProgramManifestJson = program.ProgramManifestJson,
            ProgramManifestChecksum = program.ProgramManifestChecksum,
            PublishedAt = program.PublishedAt,
            Artifacts = program.RobotProgramArtifacts
                .OrderBy(artifact => artifact.RunOrder)
                .Select(artifact =>
                {
                    snapshotsById.TryGetValue(artifact.RobotArtifactId, out var snapshot);
                    return new RobotProgramArtifactResult
                    {
                        Id = artifact.Id,
                        RobotArtifactId = artifact.RobotArtifactId,
                        RunOrder = artifact.RunOrder,
                        ParametersSchemaVersion = artifact.ParametersSchemaVersion,
                        ParametersJson = artifact.ParametersJson,
                        ArtifactCode = snapshot?.ArtifactCode,
                        ArtifactName = snapshot?.ArtifactName,
                        FileName = snapshot?.FileName,
                        Checksum = snapshot?.Checksum,
                        ArtifactStatus = snapshot?.Status.ToString()
                    };
                })
                .ToArray()
        };
    }
}

public sealed class RobotProgramArtifactResult
{
    public Guid Id { get; init; }
    public Guid RobotArtifactId { get; init; }
    public int RunOrder { get; init; }
    public int ParametersSchemaVersion { get; init; }
    public string? ParametersJson { get; init; }
    public string? ArtifactCode { get; init; }
    public string? ArtifactName { get; init; }
    public string? FileName { get; init; }
    public string? Checksum { get; init; }
    public string? ArtifactStatus { get; init; }
}
