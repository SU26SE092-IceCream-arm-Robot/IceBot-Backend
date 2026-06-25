using Domain.RobotConfiguration.Entities;

namespace Application.RobotConfiguration.Results;

public sealed class RobotArtifactResult
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string ArtifactCode { get; init; } = null!;
    public string ArtifactName { get; init; } = null!;
    public string StorageKey { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public string Checksum { get; init; } = null!;
    public string RuntimeTargetCode { get; init; } = null!;
    public string MachineModelCode { get; init; } = null!;
    public long ContentLengthBytes { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset ExportedAt { get; init; }
    public string? Description { get; init; }
    public string? MetadataJson { get; init; }

    public static RobotArtifactResult FromEntity(RobotArtifact artifact)
    {
        return new RobotArtifactResult
        {
            Id = artifact.Id,
            OrganizationId = artifact.OrganizationId,
            ArtifactCode = artifact.ArtifactCode,
            ArtifactName = artifact.ArtifactName,
            StorageKey = artifact.StorageKey,
            FileName = artifact.FileName,
            Checksum = artifact.Checksum,
            RuntimeTargetCode = artifact.RuntimeTargetCode,
            MachineModelCode = artifact.MachineModelCode,
            ContentLengthBytes = artifact.ContentLengthBytes,
            Status = artifact.Status.ToString(),
            ExportedAt = artifact.ExportedAt,
            Description = artifact.Description,
            MetadataJson = artifact.MetadataJson
        };
    }
}
