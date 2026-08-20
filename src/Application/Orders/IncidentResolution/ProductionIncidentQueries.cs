using Application.Orders.IncidentResolution.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Orders.IncidentResolution;

public sealed class ListProductionIncidentsQueryHandler(IProductionIncidentStore store)
{
    public async Task<PagedResult<ProductionIncidentResult>> HandleAsync(ListProductionIncidentsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(query.PageNumber, 1);
        var size = Math.Clamp(query.PageSize, 1, 100);
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.OrdersInterventionManage, query.UserContext);
        var (items, total) = await store.ListAsync(query.Status, query.OrganizationId, query.StoreId, query.KioskId,
            query.UserContext.IsSystemAdmin, scope.OrganizationIds, scope.StoreIds, scope.KioskIds, page, size, ct);
        return PagedResult<ProductionIncidentResult>.Success(
            items.Select(x => ProductionIncidentResultMapper.Map(x, false)), total, page, size);
    }
}

public sealed class GetProductionIncidentQueryHandler(IProductionIncidentStore store)
{
    public async Task<ApiResult<ProductionIncidentResult>> HandleAsync(GetProductionIncidentQuery query, CancellationToken ct = default)
    {
        var incident = await store.GetByIdAsync(query.IncidentId, false, ct);
        if (incident is null || incident.OrderId != query.OrderId || !CanManage(incident, query.UserContext))
            return ApiResult<ProductionIncidentResult>.Fail("Production incident not found.", 404);
        return ApiResult<ProductionIncidentResult>.Success(ProductionIncidentResultMapper.Map(incident));
    }

    internal static bool CanManage(Domain.Orders.Incidents.ProductionIncident incident,
        Application.Identity.Tokens.Claims.CurrentUserContext user) =>
        ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.OrdersInterventionManage, user,
            incident.OrganizationId, incident.StoreId, incident.KioskId);
}

