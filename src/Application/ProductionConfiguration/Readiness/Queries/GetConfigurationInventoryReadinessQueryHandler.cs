using Application.Inventory.Abstractions;
using Application.Inventory.Results;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Readiness.Services;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.ProductionConfiguration.Readiness.Queries;

public sealed class GetConfigurationInventoryReadinessQueryHandler(
    IConfigurationReleaseStore releases,
    IInventoryReadinessEvaluator readiness)
{
    public async Task<ApiResult<KioskInventoryReadinessResult>> HandleAsync(
        GetConfigurationInventoryReadinessQuery query,
        CancellationToken cancellationToken = default)
    {
        var release = await releases.GetReleaseByIdAsync(query.ConfigurationReleaseId, cancellationToken);
        if (release is null)
        {
            return ApiResult<KioskInventoryReadinessResult>.Fail("Configuration release not found.", 404);
        }

        var result = await readiness.EvaluateKioskAsync(
            query.KioskId,
            ProductionInventoryReadinessGuard.BuildRoutes(release.ExecutionRoutes),
            cancellationToken);
        if (result is null || result.OrganizationId != release.OrganizationId)
        {
            return ApiResult<KioskInventoryReadinessResult>.Fail("Kiosk or configuration release not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.InventoryView,
                query.UserContext,
                result.OrganizationId,
                result.StoreId,
                result.KioskId))
        {
            return ApiResult<KioskInventoryReadinessResult>.Fail("Access denied.", 403);
        }

        return ApiResult<KioskInventoryReadinessResult>.Success(result);
    }
}
