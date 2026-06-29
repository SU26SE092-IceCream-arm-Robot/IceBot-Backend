using Domain.Common;
using Domain.RobotConfiguration.Enums;

namespace Domain.RobotConfiguration.Entities;

public sealed class RobotArtifactTemplate : BusinessEntity
{
    public string TemplateCode { get; private set; } = null!;
    public string TemplateName { get; private set; } = null!;
    public string StorageKey { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string Checksum { get; private set; } = null!;
    public string RuntimeTargetCode { get; private set; } = null!;
    public string MachineModelCode { get; private set; } = null!;
    public long ContentLengthBytes { get; private set; }
    public RobotArtifactStatus Status { get; private set; } = RobotArtifactStatus.Draft;
    public DateTimeOffset ExportedAt { get; private set; }
    public string? Description { get; private set; }
    public string? MetadataJson { get; private set; }

    private RobotArtifactTemplate() { }

    public static RobotArtifactTemplate CreateDraft(
        string templateCode,
        string templateName,
        string storageKey,
        string fileName,
        string checksum,
        string runtimeTargetCode,
        string machineModelCode,
        long contentLengthBytes,
        DateTimeOffset exportedAt,
        string? description = null,
        string? metadataJson = null)
    {
        if (contentLengthBytes <= 0) throw new DomainRuleException("Robot artifact template content length must be greater than zero.");
        var normalizedFileName = RequireText(fileName, "Robot artifact template file name");
        if (!normalizedFileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
            throw new DomainRuleException("Robot artifact template file name must use the .lua extension.");
        var normalizedChecksum = RequireText(checksum, "Robot artifact template checksum");
        if (normalizedChecksum.Length != 64 || normalizedChecksum.Any(character => !char.IsAsciiHexDigit(character)) || normalizedChecksum.Any(char.IsUpper))
            throw new DomainRuleException("Robot artifact template checksum must be a lowercase SHA-256 hexadecimal value.");

        return new RobotArtifactTemplate
        {
            TemplateCode = RequireText(templateCode, "Robot artifact template code"),
            TemplateName = RequireText(templateName, "Robot artifact template name"),
            StorageKey = RequireText(storageKey, "Robot artifact template storage key"),
            FileName = normalizedFileName,
            Checksum = normalizedChecksum,
            RuntimeTargetCode = RequireText(runtimeTargetCode, "Robot artifact template runtime target code"),
            MachineModelCode = RequireText(machineModelCode, "Robot artifact template machine model code"),
            ContentLengthBytes = contentLengthBytes,
            ExportedAt = exportedAt,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            MetadataJson = metadataJson
        };
    }

    public void Publish()
    {
        if (Status != RobotArtifactStatus.Draft) throw new DomainRuleException("Only draft robot artifact templates can be published.");
        Status = RobotArtifactStatus.Published;
    }

    public void Retire()
    {
        if (Status == RobotArtifactStatus.Retired) return;
        if (Status != RobotArtifactStatus.Published) throw new DomainRuleException("Only published robot artifact templates can be retired.");
        Status = RobotArtifactStatus.Retired;
    }

    private static string RequireText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainRuleException($"{fieldName} is required.");
        return value.Trim();
    }
}
