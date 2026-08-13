using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Deployments.Services;
using Application.RobotConfiguration.AuthoringImports.RecipeSuggestions;
using Application.Shared.Wrappers;

namespace Application.RobotConfiguration.AuthoringImports.Workspace;

public sealed record RobotAuthoringWorkspaceResult(
    RobotAuthoringImportResult Import,
    string? ConfigurationReleaseStatus,
    IReadOnlyCollection<RobotAuthoringPackageTarget> PackageTargets,
    RobotAuthoringRecipeResolution RecipeResolution,
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
    RobotAuthoringRecipeResolver recipeResolver)
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
        var recipeResolution = await recipeResolver.ResolveAsync(
            organizationId, importId, cancellationToken);
        return ApiResult<RobotAuthoringWorkspaceResult>.Success(new RobotAuthoringWorkspaceResult(
            import,
            null,
            [],
            recipeResolution,
            null,
            [],
            import.NextActions.Select(code => new RobotAuthoringWorkspaceAction(code, false)).ToArray()));
    }
}
