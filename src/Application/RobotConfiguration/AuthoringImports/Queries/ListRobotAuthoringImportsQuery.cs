using System.Text.Json;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.RobotConfiguration.AuthoringImports;

namespace Application.RobotConfiguration.AuthoringImports.Queries;

public enum RobotAuthoringImportPublicStatus
{
    Uploaded,
    Validated,
    Materialized,
    ResourcesPublished,
    Failed,
    Discarded
}

public sealed record RobotAuthoringImportListCriteria(
    Guid OrganizationId,
    RobotAuthoringImportPublicStatus? Status,
    Guid? StoreId,
    Guid? KioskId,
    Guid? DeviceId,
    string? Search,
    int PageNumber,
    int PageSize,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null);

public sealed record RobotAuthoringImportListRow(
    Guid Id,
    Guid OrganizationId,
    Guid? StoreId,
    Guid? KioskId,
    Guid? DeviceId,
    RobotAuthoringImportStatus Status,
    string ProposedProgramCode,
    string ProposedProgramName,
    string RuntimeTargetCode,
    string MachineModelCode,
    string? ValidationReportJson,
    int ItemCount,
    Guid? MaterializedRobotProgramId,
    Guid? LinkedConfigurationReleaseId,
    Guid? ComposedRecipeId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ValidatedAt,
    DateTimeOffset? MaterializedAt,
    DateTimeOffset? PublishedAt,
    string? FailureCode,
    string? FailureMessage,
    string? CreatedByDisplayName = null);

public sealed record RobotAuthoringImportValidationSummary(
    bool CanMaterialize,
    int ErrorCount,
    int WarningCount);

public sealed record RobotAuthoringImportListItemResult(
    Guid Id,
    Guid OrganizationId,
    Guid? StoreId,
    Guid? KioskId,
    Guid? DeviceId,
    string Status,
    string ProposedProgramCode,
    string ProposedProgramName,
    string RuntimeTargetCode,
    string MachineModelCode,
    int ItemCount,
    RobotAuthoringImportValidationSummary? Validation,
    Guid? MaterializedRobotProgramId,
    Guid? LinkedConfigurationReleaseId,
    IReadOnlyCollection<string> NextActions,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ValidatedAt,
    DateTimeOffset? MaterializedAt,
    DateTimeOffset? PublishedAt,
    string? FailureCode,
    string? FailureMessage,
    string? CreatedByDisplayName)
{
    private static readonly JsonSerializerOptions ValidationJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static RobotAuthoringImportListItemResult From(RobotAuthoringImportListRow row)
    {
        var validation = ToValidationSummary(row.ValidationReportJson);
        return new RobotAuthoringImportListItemResult(
            row.Id,
            row.OrganizationId,
            row.StoreId,
            row.KioskId,
            row.DeviceId,
            RobotAuthoringImportLifecycleProjection.GetPublicStatus(row.Status, row.PublishedAt),
            row.ProposedProgramCode,
            row.ProposedProgramName,
            row.RuntimeTargetCode,
            row.MachineModelCode,
            row.ItemCount,
            validation,
            row.MaterializedRobotProgramId,
            row.LinkedConfigurationReleaseId,
            RobotAuthoringImportLifecycleProjection.GetNextActions(
                row.Status,
                validation?.CanMaterialize == true,
                row.LinkedConfigurationReleaseId,
                row.PublishedAt,
                row.ComposedRecipeId.HasValue),
            row.CreatedAt,
            row.ValidatedAt,
            row.MaterializedAt,
            row.PublishedAt,
            row.FailureCode,
            row.FailureMessage,
            row.CreatedByDisplayName);
    }

    private static RobotAuthoringImportValidationSummary? ToValidationSummary(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        try
        {
            var report = JsonSerializer.Deserialize<RobotAuthoringImportValidationReport>(value, ValidationJsonOptions);
            return report is null
                ? new RobotAuthoringImportValidationSummary(false, 1, 0)
                : new RobotAuthoringImportValidationSummary(
                    report.CanMaterialize,
                    report.Errors?.Count ?? 0,
                    report.Warnings?.Count ?? 0);
        }
        catch (JsonException)
        {
            return new RobotAuthoringImportValidationSummary(false, 1, 0);
        }
    }
}

public sealed record ListRobotAuthoringImportsQuery(
    CurrentUserContext UserContext,
    Guid OrganizationId,
    string? Status,
    Guid? StoreId,
    Guid? KioskId,
    Guid? DeviceId,
    string? Search,
    int PageNumber = 1,
    int PageSize = 20,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null);

public sealed class ListRobotAuthoringImportsQueryHandler(IRobotAuthoringImportStore store)
{
    private const int MaximumSearchLength = 100;

    public async Task<PagedResult<RobotAuthoringImportListItemResult>> HandleAsync(
        ListRobotAuthoringImportsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.ProgramRead,
                query.UserContext,
                query.OrganizationId,
                null,
                null))
        {
            return PagedResult<RobotAuthoringImportListItemResult>.Forbidden("Access denied.", pageNumber, pageSize);
        }

        var search = NormalizeSearch(query.Search);
        if (search is null && !string.IsNullOrWhiteSpace(query.Search))
        {
            return PagedResult<RobotAuthoringImportListItemResult>.Fail(
                $"Search must not exceed {MaximumSearchLength} characters.", 400, pageNumber, pageSize);
        }

        if (!TryParsePublicStatus(query.Status, out var status))
        {
            return PagedResult<RobotAuthoringImportListItemResult>.Fail(
                "Unsupported robot authoring import status.", 400, pageNumber, pageSize);
        }

        if (query.CreatedFrom.HasValue && query.CreatedTo.HasValue && query.CreatedFrom > query.CreatedTo)
        {
            return PagedResult<RobotAuthoringImportListItemResult>.Fail(
                "createdFrom must be earlier than or equal to createdTo.", 400, pageNumber, pageSize);
        }

        var criteria = new RobotAuthoringImportListCriteria(
            query.OrganizationId,
            status,
            query.StoreId,
            query.KioskId,
            query.DeviceId,
            search,
            pageNumber,
            pageSize,
            query.CreatedFrom,
            query.CreatedTo);
        var count = await store.CountImportsAsync(criteria, cancellationToken);
        var imports = await store.ListImportsAsync(criteria, cancellationToken);
        return PagedResult<RobotAuthoringImportListItemResult>.Success(
            imports.Select(RobotAuthoringImportListItemResult.From), count, pageNumber, pageSize);
    }

    private static string? NormalizeSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim();
        return normalized.Length <= MaximumSearchLength ? normalized : null;
    }

    private static bool TryParsePublicStatus(string? value, out RobotAuthoringImportPublicStatus? status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = null;
            return true;
        }

        if (Enum.TryParse<RobotAuthoringImportPublicStatus>(value, true, out var parsed))
        {
            status = parsed;
            return true;
        }

        status = null;
        return false;
    }
}

public static class RobotAuthoringImportLifecycleProjection
{
    public static string GetPublicStatus(RobotAuthoringImportStatus status, DateTimeOffset? publishedAt) =>
        status == RobotAuthoringImportStatus.Applied
            ? publishedAt.HasValue ? "ResourcesPublished" : "Materialized"
            : status.ToString();

    public static IReadOnlyCollection<string> GetNextActions(
        RobotAuthoringImportStatus status,
        bool canMaterialize,
        Guid? linkedConfigurationReleaseId,
        DateTimeOffset? publishedAt,
        bool compositionConfirmed) => status switch
    {
        RobotAuthoringImportStatus.Uploaded => ["ValidateImport", "DiscardImport"],
        RobotAuthoringImportStatus.Validated when canMaterialize => ["MaterializeImport", "DiscardImport"],
        RobotAuthoringImportStatus.Validated => ["ResolveArtifactRevisionConflict", "DiscardImport"],
        RobotAuthoringImportStatus.Applied when linkedConfigurationReleaseId.HasValue =>
            ["ReviewConfigurationReleaseDraft", "PublishConfigurationRelease"],
        RobotAuthoringImportStatus.Applied when publishedAt.HasValue && compositionConfirmed => ["CreateConfigurationReleaseDraft"],
        RobotAuthoringImportStatus.Applied when publishedAt.HasValue => ["CreateProductionBinding"],
        RobotAuthoringImportStatus.Applied => ["PublishImportResources"],
        RobotAuthoringImportStatus.Failed => ["ValidateImport", "DiscardImport"],
        _ => []
    };
}
