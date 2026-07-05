using System.Text.Json;
using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.RobotConfiguration.Enums;
using Domain.Catalog.Enums;

namespace Application.ProductionConfiguration.Commands;

public sealed class ReplaceConfigurationReleaseRoutesCommandHandler
{
    private readonly IProductionConfigurationStore _store;

    public ReplaceConfigurationReleaseRoutesCommandHandler(IProductionConfigurationStore store) => _store = store;

    public async Task<ApiResult<ConfigurationReleaseResult>> HandleAsync(
        ReplaceConfigurationReleaseRoutesCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(command.Routes);
        if (requestError is not null)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(requestError, 400);
        }

        var release = await _store.GetReleaseForEditAsync(command.ReleaseId, cancellationToken);
        if (release is null || release.OrganizationId != command.OrganizationId)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Configuration release not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleasePublish, command.UserContext, release.OrganizationId, null, null))
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Access denied.", 403);
        }

        var recipeIds = command.Routes.Select(route => route.RecipeId).Distinct().ToArray();
        var programIds = command.Routes.SelectMany(route => route.RobotBindings)
            .Select(binding => binding.RobotProgramId).Distinct().ToArray();

        var recipes = await _store.ListRecipesByIdsAsync(recipeIds, cancellationToken);
        var variantIds = recipes.Select(recipe => recipe.ProductVariantId).Distinct().ToArray();
        var variants = await _store.ListProductVariantsByIdsAsync(variantIds, cancellationToken);
        var programs = await _store.ListRobotProgramsByIdsAsync(programIds, cancellationToken);
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
                    string.IsNullOrWhiteSpace(route.RequiredCapabilitiesJson) ? null : route.RequiredCapabilitiesJson.Trim(),
                    (IReadOnlyCollection<(Guid, int, string)>)route.RobotBindings
                        .OrderBy(binding => binding.BindingOrder)
                        .Select(binding => (binding.RobotProgramId, binding.BindingOrder,
                            binding.RequiredWorkcellCapabilityCode.Trim().ToUpperInvariant()))
                        .ToArray())));

            release.UpdatedByAccountId = command.UserContext.AccountId;
            await _store.SaveReleaseReplacementAsync(removedRoutes, cancellationToken);

            var updatedRelease = await _store.GetReleaseByIdAsync(release.Id, cancellationToken)
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

        if (routes.Any(route => !IsValidOptionalJson(route.RequiredCapabilitiesJson)))
        {
            return "Execution route required capabilities must be valid JSON.";
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
        }

        return null;
    }

    private static bool IsValidOptionalJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
