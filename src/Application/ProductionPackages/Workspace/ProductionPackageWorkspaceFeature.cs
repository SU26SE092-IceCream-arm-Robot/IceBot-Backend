using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.ProductionPackages.Workspace;

public interface IProductionPackageWorkspaceStore
{
    Task<ProductionPackageWorkspaceScope?> GetScopeAsync(Guid organizationId, Guid installationId,
        CancellationToken cancellationToken);
    Task<ProductionPackageWorkspaceResult?> GetAsync(Guid organizationId, Guid installationId,
        CancellationToken cancellationToken);
}

public sealed record ProductionPackageWorkspaceScope(Guid OrganizationId, Guid? StoreId, Guid? KioskId);

public sealed record ProductionPackageWorkspaceResult(
    Guid InstallationId,
    Guid OrganizationId,
    Guid? StoreId,
    Guid? KioskId,
    string InstallationStatus,
    string OwnershipMode,
    Guid PackageId,
    string PackageCode,
    string PackageName,
    Guid PackageVersionId,
    int PackageVersion,
    IReadOnlyCollection<WorkspaceResourceResult> Products,
    IReadOnlyCollection<WorkspaceResourceResult> ProductVariants,
    IReadOnlyCollection<WorkspaceOptionResult> Options,
    IReadOnlyCollection<WorkspaceResourceResult> Recipes,
    IReadOnlyCollection<WorkspaceMenuResult> Menus,
    IReadOnlyCollection<WorkspaceArtifactResult> Artifacts,
    IReadOnlyCollection<WorkspaceProgramResult> Programs,
    WorkspaceReleaseResult? Release,
    WorkspaceTechnicalReadinessResult TechnicalReadiness,
    WorkspaceCommercialReadinessResult CommercialReadiness,
    IReadOnlyCollection<WorkspaceActionResult> RequiredActions,
    IReadOnlyCollection<WorkspaceActionResult> OptionalActions,
    IReadOnlyCollection<WorkspaceActionResult> RecoveryActions);

public sealed record WorkspaceResourceResult(Guid Id, string SourceKey, string Code, string Name, string Status);

public sealed record WorkspaceOptionResult(Guid Id, string SourceKey, string GroupCode, string Code, string Name,
    string Status, string ExecutionImpact);

public sealed record WorkspaceMenuResult(Guid Id, string Code, string Name, string Status,
    Guid? StoreId, Guid? KioskId, IReadOnlyCollection<Guid> AssignedProductVariantIds,
    IReadOnlyCollection<Guid> SellableProductVariantIds);

public sealed record WorkspaceArtifactResult(Guid Id, string SourceKey, string Code, string Name, string Status,
    Guid? TechnicalContractId, bool TechnicalContractReady);

public sealed record WorkspaceProgramArtifactResult(Guid RobotArtifactId, int RunOrder, string? RequiredOptionCode);

public sealed record WorkspaceProgramResult(Guid Id, string SourceKey, string Code, string Name, string Status,
    IReadOnlyCollection<WorkspaceProgramArtifactResult> Artifacts);

public sealed record WorkspaceReleaseResult(Guid Id, long ReleaseNumber, string Status, int RouteCount,
    string? ReleaseChecksum);

public sealed record WorkspaceTechnicalReadinessResult(
    bool IsReady,
    bool HasTargetKiosk,
    bool HasActiveExecutionEndpoint,
    string? LatestDeploymentStatus,
    IReadOnlyCollection<WorkspaceBlockerResult> Blockers);

public sealed record WorkspaceCommercialReadinessResult(
    bool IsReady,
    IReadOnlyCollection<WorkspaceBlockerResult> Blockers);

[Flags]
public enum WorkspaceReadinessImpact { None = 0, Technical = 1, Commercial = 2, Both = Technical | Commercial }

public sealed record WorkspaceBlockerResult(string Code, string Message, string? ResourceType = null,
    Guid? ResourceId = null, WorkspaceReadinessImpact Impact = WorkspaceReadinessImpact.Both);

public sealed record WorkspaceActionResult(string Code, string ResourceType, Guid? ResourceId,
    bool IsBlocked, IReadOnlyCollection<string> BlockerCodes, string? ResourceKey = null,
    int? RequiredCount = null, IReadOnlyCollection<Guid>? CandidateResourceIds = null,
    WorkspaceActionContextResult? Context = null);

public sealed record WorkspaceActionContextResult(
    Guid? ProductId = null,
    Guid? ProductVariantId = null,
    Guid? MenuId = null,
    long? OptionGroupId = null,
    Guid? KioskExecutionEndpointId = null,
    string? ExecutionProfile = null,
    IReadOnlyCollection<WorkspaceDeploymentSelectionResult>? DeploymentSelections = null);

public sealed record WorkspaceDeploymentSelectionResult(Guid ExecutionRouteId, Guid RobotProgramId);

public sealed record WorkspaceOptionAvailabilityInput(Guid OptionId, Guid ProductId, long OptionGroupId, string OptionGroupCode,
    bool GroupIsActive, bool GroupIsRequired, int MinimumSelections, bool OptionIsAvailable);

public static class ProductionPackageWorkspaceRules
{
    public static IReadOnlyCollection<WorkspaceActionResult> BuildRequiredOptionGroupActions(
        IReadOnlyCollection<WorkspaceOptionAvailabilityInput> options) => options
        .GroupBy(option => option.OptionGroupId)
        .Select(group => new { Group = group.First(), Available = group.Count(option => option.OptionIsAvailable),
            Candidates = group.Where(option => !option.OptionIsAvailable).Select(option => option.OptionId).ToArray() })
        .Where(group => group.Group.GroupIsActive && group.Group.GroupIsRequired &&
                        group.Available < group.Group.MinimumSelections)
        .Select(group => new WorkspaceActionResult("RestoreRequiredOptionGroupAvailability", "OptionGroup", null,
            false, [], group.Group.OptionGroupCode, group.Group.MinimumSelections - group.Available,
            group.Candidates, new WorkspaceActionContextResult(ProductId: group.Group.ProductId,
                OptionGroupId: group.Group.OptionGroupId))).ToArray();
}

public sealed class ProductionPackageWorkspaceService(IProductionPackageWorkspaceStore store)
{
    public async Task<ApiResult<ProductionPackageWorkspaceResult>> GetAsync(CurrentUserContext user,
        Guid organizationId, Guid installationId, CancellationToken cancellationToken)
    {
        var scope = await store.GetScopeAsync(organizationId, installationId, cancellationToken);
        if (scope is null)
            return ApiResult<ProductionPackageWorkspaceResult>.Fail("Production package installation workspace not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.PackageRead, user,
                scope.OrganizationId, scope.StoreId, scope.KioskId))
            return ApiResult<ProductionPackageWorkspaceResult>.Fail("Access denied.", 403);

        var workspace = await store.GetAsync(organizationId, installationId, cancellationToken);
        return workspace is null
            ? ApiResult<ProductionPackageWorkspaceResult>.Fail("Production package installation workspace not found.", 404)
            : ApiResult<ProductionPackageWorkspaceResult>.Success(workspace);
    }
}
