using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Domain.RobotConfiguration.Artifacts;

namespace Application.RobotConfiguration.Artifacts.Results;

public sealed class RobotArtifactResult
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? SourceRobotArtifactTemplateId { get; init; }
    public string ArtifactCode { get; init; } = null!;
    public string ArtifactName { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public string Checksum { get; init; } = null!;
    public string RuntimeTargetCode { get; init; } = null!;
    public string MachineModelCode { get; init; } = null!;
    public string RuntimeProfileSource { get; init; } = null!;
    public long ContentLengthBytes { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset ExportedAt { get; init; }
    public string? Description { get; init; }
    public string? MetadataJson { get; init; }
    public Guid? TechnicalContractId { get; init; }
    public string? TechnicalContractChecksum { get; init; }
    public bool HasTechnicalContract { get; init; }

    public static RobotArtifactResult FromEntity(RobotArtifact artifact)
    {
        return new RobotArtifactResult
        {
            Id = artifact.Id,
            OrganizationId = artifact.OrganizationId,
            SourceRobotArtifactTemplateId = artifact.SourceRobotArtifactTemplateId,
            ArtifactCode = artifact.ArtifactCode,
            ArtifactName = artifact.ArtifactName,
            FileName = artifact.FileName,
            Checksum = artifact.Checksum,
            RuntimeTargetCode = artifact.RuntimeTargetCode,
            MachineModelCode = artifact.MachineModelCode,
            RuntimeProfileSource = artifact.RuntimeProfileSource.ToString(),
            ContentLengthBytes = artifact.ContentLengthBytes,
            Status = artifact.Status.ToString(),
            ExportedAt = artifact.ExportedAt,
            Description = artifact.Description,
            MetadataJson = artifact.MetadataJson,
            TechnicalContractId = artifact.TechnicalContractId,
            TechnicalContractChecksum = artifact.TechnicalContractChecksum,
            HasTechnicalContract = artifact.TechnicalContractId.HasValue &&
                !string.IsNullOrWhiteSpace(artifact.TechnicalContractChecksum)
        };
    }
}
