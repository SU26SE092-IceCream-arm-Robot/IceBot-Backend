using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Routes.Abstractions;
using Application.ProductionConfiguration.Releases.Results;
using Application.ProductionConfiguration.Deployments.Results;
using Application.ProductionConfiguration.Routes.Support;
using Application.ProductionConfiguration.Routes.Contracts;
using Application.ProductionConfiguration.Releases.Support;
using Application.Shared.Concurrency;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.RobotConfiguration.Artifacts;
using Domain.Catalog.Enums;
using Application.Shared.Ownership;

namespace Application.ProductionConfiguration.Routes.Commands;

public sealed class ReplaceConfigurationReleaseRoutesCommandHandler
{
    private readonly IConfigurationReleaseStore _releaseStore;
    private readonly IConfigurationRouteStore _routeStore;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public ReplaceConfigurationReleaseRoutesCommandHandler(
        IConfigurationReleaseStore releaseStore,
        IConfigurationRouteStore routeStore,
        ITechnicalResourceMutationPolicy technicalOwnership,
        ITechnicalResourceMutationCoordinator mutations)
    {
        _releaseStore = releaseStore;
        _routeStore = routeStore;
        _technicalOwnership = technicalOwnership;
        _mutations = mutations;
    }

    public async Task<ApiResult<ConfigurationReleaseResult>> HandleAsync(
        ReplaceConfigurationReleaseRoutesCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(command.Routes);
        if (requestError is not null)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(requestError, 400);
        }

        return await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.ConfigurationRelease(command.ReleaseId)],
            ct => HandleLockedAsync(command, ct),
            cancellationToken);
    }

    private async Task<ApiResult<ConfigurationReleaseResult>> HandleLockedAsync(
        ReplaceConfigurationReleaseRoutesCommand command,
        CancellationToken cancellationToken)
    {

        var release = await _releaseStore.GetReleaseForEditAsync(command.ReleaseId, cancellationToken);
        if (release is null || release.OrganizationId != command.OrganizationId)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Configuration release not found.", 404);
        }

        if (!ConfigurationReleaseRevisionToken.Matches(release, command.ExpectedRevision))
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(
                "Configuration release was changed by another editor. Refresh and retry.", 409);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleasePublish, command.UserContext, release.OrganizationId, null, null))
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Access denied.", 403);
        }

        var ownershipError = await _technicalOwnership.ValidateDefinitionMutationAsync(
            TechnicalResourceKind.ConfigurationRelease, release.Id, cancellationToken);
        if (ownershipError is not null)
            return ApiResult<ConfigurationReleaseResult>.Fail(ownershipError, 409);

        var recipeIds = command.Routes.Select(route => route.RecipeId).Distinct().ToArray();
        var programIds = command.Routes.SelectMany(route => route.RobotBindings)
            .Select(binding => binding.RobotProgramId).Distinct().ToArray();

        var recipes = await _routeStore.ListRecipesByIdsAsync(recipeIds, cancellationToken);
        var variantIds = recipes.Select(recipe => recipe.ProductVariantId).Distinct().ToArray();
        var variants = await _routeStore.ListProductVariantsByIdsAsync(variantIds, cancellationToken);
        var programs = await _routeStore.ListRobotProgramsByIdsAsync(programIds, cancellationToken);
        if (variants.Count != variantIds.Length || recipes.Count != recipeIds.Length || programs.Count != programIds.Length)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("One or more route references were not found.", 400);
        }

        var variantsById = variants.ToDictionary(variant => variant.Id);
        var recipesById = recipes.ToDictionary(recipe => recipe.Id);
        var programsById = programs.ToDictionary(program => program.Id);
        foreach (var route in command.Routes)
        {
            var recipe = recipesById[route.RecipeId];
            var variant = variantsById[recipe.ProductVariantId];
            if (variant.FulfillmentType != FulfillmentType.MachineProduced)
            {
                return ApiResult<ConfigurationReleaseResult>.Fail(
                    "Execution routes require machine-produced product variants.", 400);
            }

            if (recipe.Status != RecipeStatus.Published && recipe.Status != RecipeStatus.Active)
            {
                return ApiResult<ConfigurationReleaseResult>.Fail("Execution routes require a published or active recipe.", 400);
            }

            if ((variant.Product.OrganizationId.HasValue && variant.Product.OrganizationId.Value != release.OrganizationId) ||
                (recipe.OrganizationId.HasValue && recipe.OrganizationId.Value != release.OrganizationId))
            {
                return ApiResult<ConfigurationReleaseResult>.Fail("Route product variants and recipes must belong to the release organization or be global.", 400);
            }

            var productionOptionCodes = variant.Product.OptionGroups
                .SelectMany(group => group.ProductOptions)
                .Where(option => option.DeletedAt == null &&
                    option.ExecutionImpact == ProductOptionExecutionImpact.ProductionAffecting)
                .Select(option => option.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var requestedOptionCodes = route.SupportedOptionCodes
                .Select(code => code?.Trim())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Cast<string>()
                .ToArray();
            if (requestedOptionCodes.Length != route.SupportedOptionCodes.Count ||
                requestedOptionCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != requestedOptionCodes.Length ||
                requestedOptionCodes.Any(code => !productionOptionCodes.Contains(code)))
            {
                return ApiResult<ConfigurationReleaseResult>.Fail(
                    "Supported option codes must be unique production-affecting options of the route product.", 400);
            }

            foreach (var binding in route.RobotBindings)
            {
                var program = programsById[binding.RobotProgramId];
                if (program.Status != RobotProgramStatus.Published ||
                    (program.OrganizationId.HasValue && program.OrganizationId.Value != release.OrganizationId))
                {
                    return ApiResult<ConfigurationReleaseResult>.Fail("Route bindings require published robot programs from the release organization or global scope.", 400);
                }
            }
        }

        try
        {
            var removedRoutes = release.ReplaceRoutes(command.Routes
                .OrderBy(route => route.Priority).ThenBy(route => route.RouteCode)
                .Select(route => (
                    recipesById[route.RecipeId].ProductVariantId,
                    route.RecipeId,
                    route.RouteCode.Trim().ToUpperInvariant(),
                    route.Priority,
                    ExecutionRouteCapabilityRequirementContractCodec.ToStorageJson(route.RequiredCapabilities),
                    (IReadOnlyCollection<string>)route.SupportedOptionCodes.ToArray(),
                    (IReadOnlyCollection<(Guid, int, string)>)route.RobotBindings
                        .OrderBy(binding => binding.BindingOrder)
                        .Select(binding => (binding.RobotProgramId, binding.BindingOrder,
                            binding.RequiredWorkcellCapabilityCode.Trim().ToUpperInvariant()))
                        .ToArray())));

            release.UpdatedByAccountId = command.UserContext.AccountId;
            await _routeStore.SaveReleaseReplacementAsync(removedRoutes, cancellationToken);

            var updatedRelease = await _releaseStore.GetReleaseByIdAsync(release.Id, cancellationToken)
                ?? throw new InvalidOperationException("Configuration release disappeared after route replacement.");
            return ApiResult<ConfigurationReleaseResult>.Success(
                ConfigurationReleaseResult.FromEntity(updatedRelease),
                "Configuration release routes replaced successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(ex.Message, 400);
        }
    }

    private static string? ValidateRequest(IReadOnlyCollection<ConfigurationReleaseRouteInput> routes)
    {
        if (routes.Count == 0)
        {
            return "At least one execution route is required.";
        }

        if (routes.Any(route => route.RecipeId == Guid.Empty ||
            string.IsNullOrWhiteSpace(route.RouteCode) || route.Priority < 0 || route.RobotBindings.Count == 0))
        {
            return "Every route requires a recipe, route code, non-negative priority, and at least one robot binding.";
        }

        if (routes.GroupBy(route => route.RouteCode.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            return "Execution route codes must be unique within the release.";
        }

        foreach (var route in routes)
        {
            if (route.RobotBindings.Any(binding => binding.RobotProgramId == Guid.Empty ||
                binding.BindingOrder <= 0 || string.IsNullOrWhiteSpace(binding.RequiredWorkcellCapabilityCode)))
            {
                return "Every robot binding requires a robot program, positive binding order, and workcell capability code.";
            }

            if (route.RobotBindings.GroupBy(binding => binding.BindingOrder).Any(group => group.Count() > 1))
            {
                return "Robot binding orders must be unique within each execution route.";
            }

            var requiredCapabilitiesError = ExecutionRouteRequiredCapabilitiesContract.Validate(
                ExecutionRouteCapabilityRequirementContractCodec.ToStorageJson(route.RequiredCapabilities),
                route.RobotBindings.Select(binding => binding.RequiredWorkcellCapabilityCode).ToArray());
            if (requiredCapabilitiesError is not null)
            {
                return requiredCapabilitiesError;
            }
        }

        return null;
    }
}
