using Domain.Devices.ExecutionEndpoints;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.ExecutionEndpoints.Mapping;
using Application.Devices.ExecutionEndpoints.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Catalog;
using Application.Tenants;

namespace Application.Devices.ExecutionEndpoints.Queries;

public sealed class ListExecutionEndpointsQueryHandler
{
    private readonly IExecutionEndpointStore _store;
    public ListExecutionEndpointsQueryHandler(IExecutionEndpointStore store) => _store = store;

    public async Task<ApiResult<IReadOnlyList<ExecutionEndpointResult>>> HandleAsync(
        ListExecutionEndpointsQuery query, CancellationToken cancellationToken = default)
    {
        if (!IsValidEnum<KioskExecutionProfile>(query.Profile) || !IsValidEnum<KioskExecutionEndpointStatus>(query.Status))
            return ApiResult<IReadOnlyList<ExecutionEndpointResult>>.Fail("Invalid execution endpoint profile or status.", 400);
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.DevicesView, query.UserContext);

        var endpoints = query.UserContext.IsSystemAdmin
            ? await _store.ListAsync(query.OrganizationId, query.StoreId, query.KioskId, query.Profile, query.Status, cancellationToken)
            : await _store.ListAccessibleAsync(
                scope.OrganizationIds, scope.StoreIds, scope.KioskIds,
                query.OrganizationId, query.StoreId, query.KioskId, query.Profile, query.Status, cancellationToken);

        var readiness = (await _store.ListReadinessAsync(endpoints.Select(endpoint => endpoint.Id), cancellationToken))
            .ToDictionary(projection => projection.KioskExecutionEndpointId);
        return ApiResult<IReadOnlyList<ExecutionEndpointResult>>.Success(endpoints
            .Select(endpoint => ExecutionEndpointResultMapper.ToResult(
                endpoint,
                readiness.GetValueOrDefault(endpoint.Id)))
            .ToList());
    }

    private static bool IsValidEnum<T>(string? value) where T : struct, Enum =>
        string.IsNullOrWhiteSpace(value) || (Enum.TryParse<T>(value.Trim(), true, out var parsed) && Enum.IsDefined(parsed));
}
