using System.Text.Json;
using Application.Identity.Tokens.Claims;
using Application.RobotConfiguration.AuthoringImports.Queries;
using Domain.RobotConfiguration.AuthoringImports;

namespace Application.RobotConfiguration.AuthoringImports;

public sealed class UploadRobotAuthoringImportCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long ContentLengthBytes { get; init; }
    public required Stream Content { get; init; }
}

public sealed record GetRobotAuthoringImportQuery(CurrentUserContext UserContext, Guid OrganizationId, Guid ImportId);
public sealed record ValidateRobotAuthoringImportCommand(CurrentUserContext UserContext, Guid OrganizationId, Guid ImportId);
public sealed record ResumeRobotAuthoringImportCommand(CurrentUserContext UserContext, Guid OrganizationId, Guid ImportId);
public sealed record MaterializeRobotAuthoringImportCommand(CurrentUserContext UserContext, Guid OrganizationId, Guid ImportId);
public sealed record PublishRobotAuthoringImportCommand(CurrentUserContext UserContext, Guid OrganizationId, Guid ImportId);
public sealed record DiscardRobotAuthoringImportCommand(CurrentUserContext UserContext, Guid OrganizationId, Guid ImportId);

public sealed record RobotAuthoringImportValidationIssue(string Code, string Message, string? ArtifactCode = null);

public sealed record RobotAuthoringImportValidationReport(
    bool CanMaterialize,
    IReadOnlyCollection<RobotAuthoringImportValidationIssue> Errors,
    IReadOnlyCollection<RobotAuthoringImportValidationIssue> Warnings,
    int ExistingArtifactCount,
    int NewArtifactCount,
    int ExistingContractCount,
    int NewContractCount);

public sealed record RobotAuthoringImportItemResult(
    Guid Id,
    string ArtifactCode,
    string FileName,
    string SidecarFileName,
    int RunOrder,
    string LuaChecksum,
    string SidecarChecksum,
    string Status,
    Guid? RobotArtifactId,
    Guid? TechnicalContractId,
    string? FailureCode,
    string? FailureMessage);

public sealed record RobotAuthoringImportResult(
    Guid Id,
    Guid OrganizationId,
    Guid? StoreId,
    Guid? KioskId,
    Guid? DeviceId,
    Guid ClientExportId,
    string ImportChecksum,
    int SchemaVersion,
    string Status,
    string ProposedProgramCode,
    string ProposedProgramName,
    string RuntimeTargetCode,
    string MachineModelCode,
    RobotAuthoringImportValidationReport? Validation,
    Guid? MaterializedRobotProgramId,
    Guid? LinkedConfigurationReleaseId,
    Guid? ComposedRecipeId,
    IReadOnlyCollection<string> ComposedOptionCodes,
    string? CompositionPreviewChecksum,
    IReadOnlyCollection<RobotAuthoringImportItemResult> Items,
    IReadOnlyCollection<string> NextActions,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ValidatedAt,
    DateTimeOffset? MaterializedAt,
    DateTimeOffset? PublishedAt,
    string? FailureCode,
    string? FailureMessage)
{
    public static RobotAuthoringImportResult From(RobotAuthoringImport value)
    {
        RobotAuthoringImportValidationReport? validation = null;
        if (!string.IsNullOrWhiteSpace(value.ValidationReportJson))
        {
            validation = JsonSerializer.Deserialize<RobotAuthoringImportValidationReport>(value.ValidationReportJson);
        }

        var actions = RobotAuthoringImportLifecycleProjection.GetNextActions(
            value.Status,
            validation?.CanMaterialize == true,
            value.PublishedAt);
        var publicStatus = RobotAuthoringImportLifecycleProjection.GetPublicStatus(value.Status, value.PublishedAt);

        return new RobotAuthoringImportResult(
            value.Id,
            value.OrganizationId,
            value.StoreId,
            value.KioskId,
            value.DeviceId,
            value.ClientExportId,
            value.ImportChecksum,
            value.SchemaVersion,
            publicStatus,
            value.ProposedProgramCode,
            value.ProposedProgramName,
            value.RuntimeTargetCode,
            value.MachineModelCode,
            validation,
            value.AppliedRobotProgramId,
            value.LinkedConfigurationReleaseId,
            value.ComposedRecipeId,
            value.GetComposedOptionCodes(),
            value.CompositionPreviewChecksum,
            value.Items.OrderBy(x => x.RunOrder).Select(x => new RobotAuthoringImportItemResult(
                x.Id,
                x.ArtifactCode,
                x.FileName,
                x.SidecarFileName,
                x.RunOrder,
                x.LuaChecksum,
                x.SidecarChecksum,
                x.Status.ToString(),
                x.RobotArtifactId,
                x.TechnicalContractId,
                x.FailureCode,
                x.FailureMessage)).ToArray(),
            actions,
            value.CreatedAt,
            value.ValidatedAt,
            value.AppliedAt,
            value.PublishedAt,
            value.FailureCode,
            value.FailureMessage);
    }
}
