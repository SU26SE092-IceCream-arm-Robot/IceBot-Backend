using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Enums;

namespace Application.Devices.Queries;

public sealed class ListExecutionEndpointsQueryHandler
{
    private readonly IExecutionEndpointStore _store;
    public ListExecutionEndpointsQueryHandler(IExecutionEndpointStore store) => _store = store;

    public async Task<ApiResult<IReadOnlyList<ExecutionEndpointResult>>> HandleAsync(
        ListExecutionEndpointsQuery query, CancellationToken cancellationToken = default)
    {
        if (!IsValidEnum<KioskExecutionProfile>(query.Profile) || !IsValidEnum<KioskExecutionEndpointStatus>(query.Status))
            return ApiResult<IReadOnlyList<ExecutionEndpointResult>>.Fail("Invalid execution endpoint profile or status.", 400);

        var endpoints = query.UserContext.IsSystemAdmin
            ? await _store.ListAsync(query.OrganizationId, query.StoreId, query.KioskId, query.Profile, query.Status, cancellationToken)
            : await _store.ListAccessibleAsync(
                query.UserContext.AllowedOrganizationIds, query.UserContext.AllowedStoreIds, query.UserContext.AllowedKioskIds,
                query.OrganizationId, query.StoreId, query.KioskId, query.Profile, query.Status, cancellationToken);

        return ApiResult<IReadOnlyList<ExecutionEndpointResult>>.Success(endpoints.Select(ExecutionEndpointResultMapper.ToResult).ToList());
    }

    private static bool IsValidEnum<T>(string? value) where T : struct, Enum =>
        string.IsNullOrWhiteSpace(value) || (Enum.TryParse<T>(value.Trim(), true, out var parsed) && Enum.IsDefined(parsed));
}
