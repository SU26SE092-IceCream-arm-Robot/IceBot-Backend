using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.ExecutionEndpoints.Mapping;
using Application.Devices.ExecutionEndpoints.Results;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;

namespace Application.Devices.ExecutionEndpoints.Queries;

public sealed class GetExecutionEndpointQueryHandler
{
    private readonly IExecutionEndpointStore _store;
    public GetExecutionEndpointQueryHandler(IExecutionEndpointStore store) => _store = store;

    public async Task<ApiResult<ExecutionEndpointResult>> HandleAsync(GetExecutionEndpointQuery query, CancellationToken cancellationToken = default)
    {
        var endpoint = await _store.GetByKioskIdAsync(query.KioskId, query.EndpointId, cancellationToken);
        if (endpoint is null) return ApiResult<ExecutionEndpointResult>.Fail("Execution endpoint not found.", 404);
        if (!KioskAccessRules.CanAccessKiosk(query.UserContext, endpoint.Kiosk))
            return ApiResult<ExecutionEndpointResult>.Fail("Access denied.", 403);
        var readiness = (await _store.ListReadinessAsync([endpoint.Id], cancellationToken)).SingleOrDefault();
        return ApiResult<ExecutionEndpointResult>.Success(ExecutionEndpointResultMapper.ToResult(endpoint, readiness));
    }
}
