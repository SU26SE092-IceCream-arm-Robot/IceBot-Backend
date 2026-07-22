using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Deployments.Services;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionPackages.Ownership;
using Application.Shared.Wrappers;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionPackages;

namespace Application.RobotConfiguration.AuthoringImports.Workspace;

public sealed record RobotAuthoringWorkspaceResult(
    RobotAuthoringImportResult Import,
    string? ConfigurationReleaseStatus,
    IReadOnlyCollection<RobotAuthoringPackageTarget> PackageTargets,
    ConfigurationDeploymentPreview? DeploymentPreview,
    IReadOnlyCollection<RobotAuthoringWorkspaceBlocker> Blockers,
    IReadOnlyCollection<RobotAuthoringWorkspaceAction> Actions);

public sealed record RobotAuthoringPackageTarget(
    Guid InstallationId,
    string OwnershipMode,
    string Status,
    bool RequiresForkBeforeTechnicalMutation);

public sealed record RobotAuthoringWorkspaceAction(
    string Code,
    bool IsBlocked,
    string? BlockerCode = null,
    Guid? ResourceId = null);

public sealed record RobotAuthoringWorkspaceBlocker(
    string Code,
    string Message,
    int? StatusCode = null);

public sealed class RobotAuthoringWorkspaceHandler(
    RobotAuthoringImportHandlers imports,
    IProductionPackageTechnicalOwnershipStore packageOwnership,
    IConfigurationReleaseStore releases,
    IConfigurationDeploymentPreviewService deploymentPreview)
{
    public async Task<ApiResult<RobotAuthoringWorkspaceResult>> HandleAsync(
        CurrentUserContext user,
        Guid organizationId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var importResult = await imports.GetAsync(
            new GetRobotAuthoringImportQuery(user, organizationId, importId), cancellationToken);
        if (!importResult.Succeeded || importResult.Data is null)
            return ApiResult<RobotAuthoringWorkspaceResult>.Fail(
                importResult.Message ?? "Robot authoring import not found.", importResult.StatusCode);

        var import = importResult.Data;
        var blockers = new List<RobotAuthoringWorkspaceBlocker>();
        ConfigurationRelease? release = null;
        if (import.LinkedConfigurationReleaseId.HasValue)
        {
            release = await releases.GetReleaseByIdAsync(
                import.LinkedConfigurationReleaseId.Value, cancellationToken);
            if (release is null)
                blockers.Add(new("ConfigurationReleaseNotFound",
                    "The linked configuration release no longer exists.", 404));
        }

        var recipeId = import.ComposedRecipeId ?? ResolveSingleReleaseRecipeId(release);
        IReadOnlyCollection<ProductionPackageResourceOwner> owners = [];
        if (recipeId.HasValue)
        {
            owners = await packageOwnership.ListOwnersAsync(
                ProductionPackageResourceKind.Recipe, recipeId.Value, cancellationToken);
        }

        var targets = owners
            .Where(owner => owner.OrganizationId == organizationId)
            .Select(owner => new RobotAuthoringPackageTarget(
                owner.InstallationId,
                owner.OwnershipMode,
                owner.Status,
                string.Equals(owner.OwnershipMode, ProductionPackageOwnershipMode.PackageManaged.ToString(),
                    StringComparison.Ordinal)))
            .ToArray();

        ConfigurationDeploymentPreview? preview = null;
        var releaseStatus = release?.Status;
        if (release?.Status == ConfigurationReleaseStatus.Published)
        {
            if (!import.KioskId.HasValue)
            {
                blockers.Add(new("ImportHasNoKioskScope",
                    "Select a kiosk before previewing deployment."));
            }
            else
            {
                var previewResult = await deploymentPreview.HandleAsync(
                    user, import.KioskId.Value, release.Id, null, [], cancellationToken);
                if (previewResult.Succeeded)
                    preview = previewResult.Data;
                else
                    blockers.Add(new("DeploymentPreviewUnavailable",
                        previewResult.Message ?? "Deployment preview could not be built.",
                        previewResult.StatusCode));

                if (preview is not null && !preview.Endpoints.Any(endpoint => endpoint.IsEligible))
                {
                    blockers.AddRange(preview.Endpoints
                        .SelectMany(endpoint => endpoint.Blockers)
                        .Select(blocker => new RobotAuthoringWorkspaceBlocker(blocker.Code, blocker.Message)));
                }
            }
        }

        var distinctBlockers = blockers.DistinctBy(blocker => (blocker.Code, blocker.Message)).ToArray();
        var actions = BuildActions(import, releaseStatus, preview, distinctBlockers);
        return ApiResult<RobotAuthoringWorkspaceResult>.Success(new RobotAuthoringWorkspaceResult(
            import,
            releaseStatus?.ToString(),
            targets,
            preview,
            distinctBlockers,
            actions));
    }

    public static IReadOnlyCollection<RobotAuthoringWorkspaceAction> BuildActions(
        RobotAuthoringImportResult import,
        ConfigurationReleaseStatus? releaseStatus,
        ConfigurationDeploymentPreview? preview,
        IReadOnlyCollection<RobotAuthoringWorkspaceBlocker> blockers)
    {
        var releaseActions = new HashSet<string>(StringComparer.Ordinal)
        {
            "CreateConfigurationReleaseDraft",
            "ReviewConfigurationReleaseDraft",
            "PublishConfigurationRelease"
        };
        var actions = import.NextActions
            .Where(code => !releaseActions.Contains(code))
            .Select(code => new RobotAuthoringWorkspaceAction(code, false))
            .ToList();

        if (import.LinkedConfigurationReleaseId.HasValue && releaseStatus == ConfigurationReleaseStatus.Draft)
        {
            actions.Add(new("ReviewConfigurationReleaseDraft", false,
                ResourceId: import.LinkedConfigurationReleaseId));
            actions.Add(new("PublishConfigurationRelease", false,
                ResourceId: import.LinkedConfigurationReleaseId));
        }
        else if (!import.LinkedConfigurationReleaseId.HasValue && import.PublishedAt.HasValue)
        {
            actions.Add(new("CreateConfigurationReleaseDraft", false));
        }

        if (releaseStatus == ConfigurationReleaseStatus.Published && !import.KioskId.HasValue)
            actions.Add(new("SelectDeploymentKiosk", false));
        if (preview is not null)
        {
            if (preview.RequiresEndpointSelection)
                actions.Add(new("SelectExecutionEndpoint", false));
            else if (preview.Endpoints.Any(endpoint => endpoint.IsEligible))
                actions.Add(new("ConfirmDeployment", false));
            else
                actions.Add(new("ResolveDeploymentBlockers", true, "NoEligibleExecutionEndpoint"));
        }
        else if (releaseStatus == ConfigurationReleaseStatus.Published && import.KioskId.HasValue && blockers.Count > 0)
        {
            actions.Add(new("ResolveDeploymentBlockers", true, blockers.First().Code));
        }

        return actions.DistinctBy(action => (action.Code, action.ResourceId)).ToArray();
    }

    private static Guid? ResolveSingleReleaseRecipeId(ConfigurationRelease? release)
    {
        if (release is null) return null;
        var recipeIds = release.ExecutionRoutes.Select(route => route.RecipeId).Distinct().Take(2).ToArray();
        return recipeIds.Length == 1 ? recipeIds[0] : null;
    }
}
