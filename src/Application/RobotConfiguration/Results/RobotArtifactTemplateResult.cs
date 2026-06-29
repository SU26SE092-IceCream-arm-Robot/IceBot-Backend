using Domain.RobotConfiguration.Entities;

namespace Application.RobotConfiguration.Results;

public sealed class RobotArtifactTemplateResult
{
    public Guid Id { get; init; }
    public string TemplateCode { get; init; } = string.Empty;
    public string TemplateName { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Checksum { get; init; } = string.Empty;
    public string RuntimeTargetCode { get; init; } = string.Empty;
    public string MachineModelCode { get; init; } = string.Empty;
    public long ContentLengthBytes { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset ExportedAt { get; init; }
    public string? Description { get; init; }
    public string? MetadataJson { get; init; }

    public static RobotArtifactTemplateResult FromEntity(RobotArtifactTemplate template) => new()
    {
        Id = template.Id,
        TemplateCode = template.TemplateCode,
        TemplateName = template.TemplateName,
        FileName = template.FileName,
        Checksum = template.Checksum,
        RuntimeTargetCode = template.RuntimeTargetCode,
        MachineModelCode = template.MachineModelCode,
        ContentLengthBytes = template.ContentLengthBytes,
        Status = template.Status.ToString(),
        ExportedAt = template.ExportedAt,
        Description = template.Description,
        MetadataJson = template.MetadataJson
    };
}

public sealed class BulkRobotArtifactTemplateUploadResult
{
    public int UploadedCount { get; init; }
    public int ExistingCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyCollection<BulkRobotArtifactTemplateUploadItemResult> Items { get; init; } = [];
}

public sealed class BulkRobotArtifactTemplateUploadItemResult
{
    public string FileName { get; init; } = string.Empty;
    public bool Succeeded { get; init; }
    public bool WasExisting { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public RobotArtifactTemplateResult? Template { get; init; }
}

public sealed class RobotArtifactTemplateDiscardResult
{
    public Guid RobotArtifactTemplateId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public bool ObjectDeleted { get; init; }
}
