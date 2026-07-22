using Domain.Common;
using System.Text.Json;

namespace Domain.RobotConfiguration.AuthoringImports;

public enum RobotAuthoringImportStatus
{
    Uploaded = 0,
    Validated = 1,
    Applied = 2,
    Failed = 3,
    Discarded = 4
}

public enum RobotAuthoringImportItemStatus
{
    Staged = 0,
    Existing = 1,
    Created = 2,
    Conflict = 3,
    Failed = 4
}

public sealed class RobotAuthoringImport : BusinessEntity
{
    private readonly List<RobotAuthoringImportItem> _items = [];

    public Guid OrganizationId { get; private set; }
    public Guid? StoreId { get; private set; }
    public Guid? KioskId { get; private set; }
    public Guid? DeviceId { get; private set; }
    public Guid ClientExportId { get; private set; }
    public string ImportChecksum { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;
    public int SchemaVersion { get; private set; }
    public RobotAuthoringImportStatus Status { get; private set; }
    public string ProposedProgramCode { get; private set; } = null!;
    public string ProposedProgramName { get; private set; } = null!;
    public string RuntimeTargetCode { get; private set; } = null!;
    public string MachineModelCode { get; private set; } = null!;
    public string StagingStorageKey { get; private set; } = null!;
    public string? ValidationReportJson { get; private set; }
    public Guid? AppliedRobotProgramId { get; private set; }
    public Guid? LinkedConfigurationReleaseId { get; private set; }
    public Guid? ComposedRecipeId { get; private set; }
    public string? ComposedOptionCodesJson { get; private set; }
    public string? CompositionPreviewChecksum { get; private set; }
    public DateTimeOffset? ValidatedAt { get; private set; }
    public DateTimeOffset? AppliedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? ReleaseLinkedAt { get; private set; }
    public DateTimeOffset? CompositionConfirmedAt { get; private set; }
    public DateTimeOffset? DiscardedAt { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }
    public IReadOnlyCollection<RobotAuthoringImportItem> Items => _items;

    private RobotAuthoringImport() { }

    public static RobotAuthoringImport Create(
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? deviceId,
        Guid clientExportId,
        string importChecksum,
        string idempotencyKey,
        int schemaVersion,
        string programCode,
        string programName,
        string runtimeTargetCode,
        string machineModelCode,
        string stagingStorageKey,
        Guid actorId)
    {
        if (organizationId == Guid.Empty || clientExportId == Guid.Empty)
            throw new DomainRuleException("Organization and client export identities are required.");
        if (schemaVersion != 1)
            throw new DomainRuleException("Only robot authoring import schema version 1 is supported.");

        return new RobotAuthoringImport
        {
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            DeviceId = deviceId,
            ClientExportId = clientExportId,
            ImportChecksum = RequireText(importChecksum, "Import checksum").ToLowerInvariant(),
            IdempotencyKey = RequireText(idempotencyKey, "Idempotency key"),
            SchemaVersion = schemaVersion,
            ProposedProgramCode = NormalizeCode(programCode),
            ProposedProgramName = RequireText(programName, "Program name"),
            RuntimeTargetCode = NormalizeCode(runtimeTargetCode),
            MachineModelCode = NormalizeCode(machineModelCode),
            StagingStorageKey = RequireText(stagingStorageKey, "Staging storage key"),
            Status = RobotAuthoringImportStatus.Uploaded,
            CreatedByAccountId = actorId
        };
    }

    public void AddItem(string artifactCode, string fileName, string sidecarFileName, int runOrder,
        string luaChecksum, string sidecarChecksum)
    {
        if (Status != RobotAuthoringImportStatus.Uploaded)
            throw new DomainRuleException("Import items can only be staged while the import is Uploaded.");
        if (_items.Any(x => x.RunOrder == runOrder || x.ArtifactCode == NormalizeCode(artifactCode)))
            throw new DomainRuleException("Import artifact codes and run order values must be unique.");
        _items.Add(RobotAuthoringImportItem.Create(artifactCode, fileName, sidecarFileName, runOrder,
            luaChecksum, sidecarChecksum));
    }

    public void MarkValidated(string validationReportJson, DateTimeOffset now, Guid actorId)
    {
        if (Status is RobotAuthoringImportStatus.Applied or RobotAuthoringImportStatus.Discarded)
            throw new DomainRuleException("Applied or discarded imports cannot be validated.");
        ValidationReportJson = RequireText(validationReportJson, "Validation report");
        Status = RobotAuthoringImportStatus.Validated;
        ValidatedAt = now;
        FailureCode = null;
        FailureMessage = null;
        Touch(now, actorId);
    }

    public void MarkApplied(Guid programId, DateTimeOffset now, Guid actorId)
    {
        if (Status != RobotAuthoringImportStatus.Validated)
            throw new DomainRuleException("Only a validated import can be applied.");
        if (programId == Guid.Empty)
            throw new DomainRuleException("Applied robot program id is required.");
        AppliedRobotProgramId = programId;
        Status = RobotAuthoringImportStatus.Applied;
        AppliedAt = now;
        Touch(now, actorId);
    }

    public void MarkPublished(DateTimeOffset now, Guid actorId)
    {
        if (Status != RobotAuthoringImportStatus.Applied || !AppliedRobotProgramId.HasValue)
            throw new DomainRuleException("Only an applied import can complete publication.");
        PublishedAt = now;
        Touch(now, actorId);
    }

    public void LinkConfigurationRelease(Guid configurationReleaseId, DateTimeOffset now, Guid actorId)
    {
        if (!PublishedAt.HasValue || Status != RobotAuthoringImportStatus.Applied)
            throw new DomainRuleException("Only an import with published resources can be linked to a configuration release.");
        if (configurationReleaseId == Guid.Empty)
            throw new DomainRuleException("Linked configuration release id is required.");
        if (LinkedConfigurationReleaseId.HasValue && LinkedConfigurationReleaseId != configurationReleaseId)
            throw new DomainRuleException("Robot authoring import is already linked to another configuration release.");

        LinkedConfigurationReleaseId = configurationReleaseId;
        ReleaseLinkedAt ??= now;
        Touch(now, actorId);
    }

    public void ConfirmComposition(Guid recipeId, IReadOnlyCollection<string> optionCodes, string previewChecksum,
        DateTimeOffset now, Guid actorId)
    {
        if (Status != RobotAuthoringImportStatus.Applied || !AppliedRobotProgramId.HasValue || PublishedAt.HasValue)
            throw new DomainRuleException("Composition can only be confirmed for an applied, unpublished import.");
        if (recipeId == Guid.Empty || string.IsNullOrWhiteSpace(previewChecksum))
            throw new DomainRuleException("Composition recipe and preview checksum are required.");
        var normalizedOptions = optionCodes.Select(NormalizeCode).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (normalizedOptions.Length != optionCodes.Count)
            throw new DomainRuleException("Composition option codes must be non-empty and unique.");

        ComposedRecipeId = recipeId;
        ComposedOptionCodesJson = JsonSerializer.Serialize(normalizedOptions);
        CompositionPreviewChecksum = previewChecksum.Trim().ToLowerInvariant();
        CompositionConfirmedAt = now;
        Touch(now, actorId);
    }

    public IReadOnlyCollection<string> GetComposedOptionCodes() =>
        string.IsNullOrWhiteSpace(ComposedOptionCodesJson)
            ? []
            : JsonSerializer.Deserialize<string[]>(ComposedOptionCodesJson) ?? [];

    public void MarkFailed(string code, string message, DateTimeOffset now, Guid actorId)
    {
        if (Status is RobotAuthoringImportStatus.Applied or RobotAuthoringImportStatus.Discarded)
            throw new DomainRuleException("Applied or discarded imports cannot fail.");
        Status = RobotAuthoringImportStatus.Failed;
        FailureCode = NormalizeCode(code);
        FailureMessage = RequireText(message, "Failure message");
        Touch(now, actorId);
    }

    public void Discard(DateTimeOffset now, Guid actorId)
    {
        if (Status == RobotAuthoringImportStatus.Applied)
            throw new DomainRuleException("Applied imports are retained as authoring provenance.");
        Status = RobotAuthoringImportStatus.Discarded;
        DiscardedAt = now;
        Touch(now, actorId);
    }

    private void Touch(DateTimeOffset now, Guid actorId)
    {
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    private static string RequireText(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainRuleException($"{name} is required.") : value.Trim();

    private static string NormalizeCode(string value) => RequireText(value, "Code").ToUpperInvariant();
}

public sealed class RobotAuthoringImportItem : BusinessEntity
{
    public Guid RobotAuthoringImportId { get; private set; }
    public string ArtifactCode { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string SidecarFileName { get; private set; } = null!;
    public int RunOrder { get; private set; }
    public string LuaChecksum { get; private set; } = null!;
    public string SidecarChecksum { get; private set; } = null!;
    public RobotAuthoringImportItemStatus Status { get; private set; }
    public Guid? RobotArtifactId { get; private set; }
    public Guid? TechnicalContractId { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }
    public RobotAuthoringImport RobotAuthoringImport { get; private set; } = null!;

    private RobotAuthoringImportItem() { }

    internal static RobotAuthoringImportItem Create(string artifactCode, string fileName, string sidecarFileName,
        int runOrder, string luaChecksum, string sidecarChecksum)
    {
        if (runOrder <= 0) throw new DomainRuleException("Run order must be positive.");
        return new RobotAuthoringImportItem
        {
            ArtifactCode = artifactCode.Trim().ToUpperInvariant(),
            FileName = fileName.Trim(),
            SidecarFileName = sidecarFileName.Trim(),
            RunOrder = runOrder,
            LuaChecksum = luaChecksum.Trim().ToLowerInvariant(),
            SidecarChecksum = sidecarChecksum.Trim().ToLowerInvariant(),
            Status = RobotAuthoringImportItemStatus.Staged
        };
    }

    public void MarkResolved(Guid artifactId, Guid technicalContractId, bool created)
    {
        RobotArtifactId = artifactId;
        TechnicalContractId = technicalContractId;
        Status = created ? RobotAuthoringImportItemStatus.Created : RobotAuthoringImportItemStatus.Existing;
        FailureCode = null;
        FailureMessage = null;
    }

    public void MarkConflict(string code, string message)
    {
        Status = RobotAuthoringImportItemStatus.Conflict;
        FailureCode = code.Trim().ToUpperInvariant();
        FailureMessage = message.Trim();
    }
}
