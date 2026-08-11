using Domain.Common;

namespace Domain.RobotConfiguration.Artifacts;

public class RobotArtifact : RobotConfigurationEntity
{
    public Guid OrganizationId { get; private set; }

    public Guid? SourceRobotArtifactTemplateId { get; private set; }

    public string ArtifactCode { get; private set; } = null!;

    public string ArtifactName { get; private set; } = null!;

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
    public Guid? TechnicalContractId { get; private set; }
    public string? TechnicalContractChecksum { get; private set; }

    private RobotArtifact()
    {
    }

    public static RobotArtifact CreateDraft(
        Guid organizationId,
        string artifactCode,
        string artifactName,
        string storageKey,
        string fileName,
        string checksum,
        string runtimeTargetCode,
        string machineModelCode,
        long contentLengthBytes,
        DateTimeOffset exportedAt,
        string? description = null,
        string? metadataJson = null,
        Guid? sourceRobotArtifactTemplateId = null,
        Guid? technicalContractId = null,
        string? technicalContractChecksum = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainRuleException("Robot artifact organization id is required.");
        }

        if (contentLengthBytes <= 0)
        {
            throw new DomainRuleException("Robot artifact content length must be greater than zero.");
        }

        return new RobotArtifact
        {
            OrganizationId = organizationId,
            SourceRobotArtifactTemplateId = sourceRobotArtifactTemplateId,
            TechnicalContractId = technicalContractId,
            TechnicalContractChecksum = string.IsNullOrWhiteSpace(technicalContractChecksum) ? null : technicalContractChecksum.Trim(),
            ArtifactCode = RequireText(artifactCode, "Robot artifact code"),
            ArtifactName = RequireText(artifactName, "Robot artifact name"),
            StorageKey = RequireText(storageKey, "Robot artifact storage key"),
            FileName = RequireLuaFileName(fileName),
            Checksum = RequireSha256Checksum(checksum, "Robot artifact checksum"),
            RuntimeTargetCode = RequireText(runtimeTargetCode, "Robot artifact runtime target code"),
            MachineModelCode = RequireText(machineModelCode, "Robot artifact machine model code"),
            ContentLengthBytes = contentLengthBytes,
            ExportedAt = exportedAt,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            MetadataJson = metadataJson
        };
    }

    public void AssignTechnicalContract(Guid technicalContractId, string checksum)
    {
        if (Status != RobotArtifactStatus.Draft)
            throw new DomainRuleException("Only Draft robot artifacts can change technical contract.");
        if (technicalContractId == Guid.Empty || string.IsNullOrWhiteSpace(checksum))
            throw new DomainRuleException("Published technical contract identity and checksum are required.");
        TechnicalContractId = technicalContractId;
        TechnicalContractChecksum = checksum.Trim();
    }

    public void Publish()
    {
        if (Status != RobotArtifactStatus.Draft)
        {
            throw new DomainRuleException("Only draft robot artifacts can be published.");
        }

        Status = RobotArtifactStatus.Published;
    }

    public void Retire()
    {
        if (Status == RobotArtifactStatus.Retired)
        {
            return;
        }

        if (Status == RobotArtifactStatus.Draft)
        {
            throw new DomainRuleException("Draft robot artifacts should be deleted or disabled, not retired.");
        }

        Status = RobotArtifactStatus.Retired;
    }

    private static string RequireText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string RequireSha256Checksum(string value, string fieldName)
    {
        var normalized = RequireText(value, fieldName);
        if (normalized.Length != 64 || normalized.Any(character => !char.IsAsciiHexDigit(character)) || normalized.Any(char.IsUpper))
        {
            throw new DomainRuleException($"{fieldName} must be a lowercase SHA-256 hexadecimal value.");
        }

        return normalized;
    }

    private static string RequireLuaFileName(string fileName)
    {
        var normalized = RequireText(fileName, "Robot artifact file name");
        if (!normalized.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainRuleException("Robot artifact file name must use the .lua extension.");
        }

        return normalized;
    }
}
