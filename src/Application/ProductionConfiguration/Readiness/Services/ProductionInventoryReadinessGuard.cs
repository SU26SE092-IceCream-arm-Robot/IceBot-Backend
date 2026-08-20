using Application.Inventory.Abstractions;
using Application.Inventory.Results;
using Domain.ProductionConfiguration.Entities;
using Microsoft.Extensions.Options;

namespace Application.ProductionConfiguration.Readiness.Services;

public sealed class ProductionInventoryReadinessGuard(
    IInventoryReadinessEvaluator evaluator,
    IOptions<InventoryReadinessPolicyOptions> options)
{
    private readonly InventoryReadinessPolicyOptions _options = options.Value;

    public async Task<ProductionInventoryReadinessAssessment> EvaluatePublishAsync(
        ConfigurationRelease release,
        CancellationToken cancellationToken = default)
    {
        var routes = BuildRoutes(release.ExecutionRoutes);
        var results = await evaluator.EvaluateOrganizationAsync(release.OrganizationId, routes, cancellationToken);
        return BuildAssessment(_options.PublishPolicy, results);
    }

    public async Task<ProductionInventoryReadinessAssessment> EvaluateDeployAsync(
        ConfigurationRelease release,
        Guid kioskId,
        IReadOnlyCollection<Guid>? selectedRouteIds = null,
        CancellationToken cancellationToken = default)
    {
        var routes = release.ExecutionRoutes.AsEnumerable();
        if (selectedRouteIds is not null)
        {
            routes = routes.Where(route => selectedRouteIds.Contains(route.Id));
        }

        var result = await evaluator.EvaluateKioskAsync(kioskId, BuildRoutes(routes), cancellationToken);
        var results = result is null ? [] : new[] { result };
        return BuildAssessment(_options.DeployPolicy, results);
    }

    public static IReadOnlyCollection<InventoryReadinessRouteInput> BuildRoutes(
        IEnumerable<ExecutionRoute> routes) =>
        routes.Select(route => new InventoryReadinessRouteInput(
            route.Id,
            route.RouteCode,
            route.ProductVariant.ProductId,
            route.RecipeId,
            route.GetSupportedOptionCodes().ToHashSet(StringComparer.OrdinalIgnoreCase),
            route.ProductVariant.Product.OrganizationId,
            route.ProductVariant.Product.StoreId,
            route.ProductVariant.Product.KioskId,
            route.Recipe.OrganizationId,
            route.Recipe.StoreId,
            route.Recipe.KioskId)).ToArray();

    private static ProductionInventoryReadinessAssessment BuildAssessment(
        InventoryReadinessPolicy policy,
        IReadOnlyCollection<KioskInventoryReadinessResult> results)
    {
        // Inventory tracking is optional. Once a kiosk configures a Cloud
        // balance, its recorded state becomes authoritative for publication
        // and deployment policy evaluation.
        var trackedResults = results
            .Where(result => result.HasConfiguredInventoryBalance)
            .ToArray();
        var notReady = trackedResults.Where(result => !result.IsReady).ToArray();
        return new ProductionInventoryReadinessAssessment(
            policy,
            notReady.Length > 0,
            policy == InventoryReadinessPolicy.Block && notReady.Length > 0,
            results);
    }
}

public sealed record ProductionInventoryReadinessAssessment(
    InventoryReadinessPolicy Policy,
    bool HasWarnings,
    bool IsBlocked,
    IReadOnlyCollection<KioskInventoryReadinessResult> Results);
