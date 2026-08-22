using System.Text.Json;
using Application.ProductionConfiguration.Routes.Support;
using Application.SalesCatalog.ReadModels;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionConfiguration.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SalesCatalog.Persistence;

internal static class ExecutionRouteAvailabilityReader
{
    internal static async Task<IReadOnlyList<AvailableExecutionRoute>> ListAsync(
        IceBotDbContext dbContext,
        Guid kioskId,
        IReadOnlyCollection<ActiveProductionRouteOptionPolicyKey> keys,
        DateTimeOffset readinessReceivedAfter,
        CancellationToken cancellationToken)
    {
        if (keys.Count == 0) return [];

        var endpointRows = await dbContext.ExecutionEndpointReadinessProjections.AsNoTracking()
            .Where(readiness =>
                readiness.KioskId == kioskId &&
                readiness.Readiness == ExecutionReadinessState.Ready &&
                readiness.Safety == ExecutionSafetyState.Safe &&
                readiness.CloudReceivedAt >= readinessReceivedAfter &&
                readiness.KioskExecutionEndpoint.Status == KioskExecutionEndpointStatus.Active &&
                ((readiness.KioskExecutionEndpoint.ExecutionProfile == KioskExecutionProfile.FullEdge &&
                  readiness.KioskExecutionEndpoint.ActiveConfigurationReleaseId != null) ||
                 (readiness.KioskExecutionEndpoint.ExecutionProfile != KioskExecutionProfile.FullEdge &&
                  readiness.KioskExecutionEndpoint.ActiveArtifactSetReleaseId != null)))
            .Select(readiness => new EndpointRow(
                readiness.Id,
                readiness.KioskExecutionEndpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
                    ? readiness.KioskExecutionEndpoint.ActiveConfigurationReleaseId
                    : readiness.KioskExecutionEndpoint.ActiveArtifactSetReleaseId))
            .ToListAsync(cancellationToken);

        var readinessIds = endpointRows.Select(row => row.ReadinessId).ToArray();
        var availableCapabilities = await dbContext.ExecutionEndpointCapabilityProjections.AsNoTracking()
            .Where(capability => readinessIds.Contains(capability.ExecutionEndpointReadinessProjectionId) &&
                capability.IsAvailable)
            .Select(capability => new { capability.ExecutionEndpointReadinessProjectionId, capability.CapabilityCode })
            .ToListAsync(cancellationToken);
        var capabilitiesByReadiness = availableCapabilities
            .GroupBy(capability => capability.ExecutionEndpointReadinessProjectionId)
            .ToDictionary(group => group.Key, group => group.Select(value => value.CapabilityCode).ToArray());
        var endpointEvidence = endpointRows.Select(row => new EndpointEvidence(
            row.ReleaseId,
            capabilitiesByReadiness.GetValueOrDefault(row.ReadinessId) ?? [])).ToArray();

        var releaseIds = endpointEvidence.Select(evidence => evidence.ReleaseId!.Value).Distinct().ToArray();
        if (releaseIds.Length == 0) return [];

        var variantIds = keys.Select(key => key.ProductVariantId).Distinct().ToArray();
        var recipeIds = keys.Select(key => key.RecipeId).Distinct().ToArray();
        var requestedKeys = keys.ToHashSet();
        var routeRows = await dbContext.ExecutionRoutes.AsNoTracking()
            .Where(route => releaseIds.Contains(route.ConfigurationReleaseId) &&
                route.ConfigurationRelease.Status == ConfigurationReleaseStatus.Published &&
                variantIds.Contains(route.ProductVariantId) && recipeIds.Contains(route.RecipeId) &&
                route.RobotBindings.Any())
            .OrderBy(route => route.Priority).ThenBy(route => route.RouteCode)
            .Select(route => new RouteRow(
                route.Id,
                route.ConfigurationReleaseId,
                route.ProductVariantId,
                route.RecipeId,
                route.SupportedOptionCodesJson,
                route.RequiredCapabilitiesJson))
            .ToListAsync(cancellationToken);
        var routeIds = routeRows.Select(route => route.Id).ToArray();
        var bindingRows = await dbContext.ExecutionRouteRobotBindings.AsNoTracking()
            .Where(binding => routeIds.Contains(binding.ExecutionRouteId))
            .OrderBy(binding => binding.BindingOrder)
            .Select(binding => new { binding.ExecutionRouteId, binding.RequiredCapabilityCodesJson })
            .ToListAsync(cancellationToken);
        var bindingCapabilitiesByRoute = bindingRows
            .GroupBy(binding => binding.ExecutionRouteId)
            .ToDictionary(group => group.Key,
                group => group.Select(binding => binding.RequiredCapabilityCodesJson).ToArray());
        var candidates = routeRows.Select(route => new RouteEvidence(
            route.Id,
            route.ReleaseId,
            route.ProductVariantId,
            route.RecipeId,
            route.SupportedOptionCodesJson,
            route.RequiredCapabilitiesJson,
            bindingCapabilitiesByRoute.GetValueOrDefault(route.Id) ?? [])).ToArray();

        return candidates
            .Where(route => requestedKeys.Contains(new(route.ProductVariantId, route.RecipeId)))
            .Where(route => !ExecutionRouteRequiredCapabilitiesContract.HasUnverifiableRequiredVersion(
                route.RequiredCapabilitiesJson))
            .Where(route => endpointEvidence.Any(endpoint => endpoint.ReleaseId == route.ReleaseId &&
                AllBindingCapabilitiesAvailable(route.BindingCapabilitySetsJson, endpoint.AvailableCapabilityCodes)))
            .Select(route => new AvailableExecutionRoute(
                route.Id,
                route.ProductVariantId,
                route.RecipeId,
                (JsonSerializer.Deserialize<string[]>(route.SupportedOptionCodesJson) ?? [])
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static bool AllBindingCapabilitiesAvailable(
        IReadOnlyCollection<string> bindingCapabilitySetsJson,
        IReadOnlyCollection<string> availableCapabilityCodes)
    {
        var available = availableCapabilityCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return bindingCapabilitySetsJson.All(json =>
            (JsonSerializer.Deserialize<string[]>(json) ?? []).All(available.Contains));
    }

    private sealed record EndpointRow(Guid ReadinessId, Guid? ReleaseId);
    private sealed record EndpointEvidence(Guid? ReleaseId, string[] AvailableCapabilityCodes);
    private sealed record RouteRow(Guid Id, Guid ReleaseId, Guid ProductVariantId, Guid RecipeId,
        string SupportedOptionCodesJson, string? RequiredCapabilitiesJson);
    private sealed record RouteEvidence(Guid Id, Guid ReleaseId, Guid ProductVariantId, Guid RecipeId,
        string SupportedOptionCodesJson, string? RequiredCapabilitiesJson, string[] BindingCapabilitySetsJson);
}

internal sealed record AvailableExecutionRoute(
    Guid Id,
    Guid ProductVariantId,
    Guid RecipeId,
    IReadOnlySet<string> SupportedOptionCodes);
