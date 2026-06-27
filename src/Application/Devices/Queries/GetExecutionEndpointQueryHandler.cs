using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;

namespace Application.Devices.Queries;

public sealed class GetExecutionEndpointQueryHandler
{
    private readonly IExecutionEndpointStore _store;
    public GetExecutionEndpointQueryHandler(IExecutionEndpointStore store) => _store = store;

    public async Task<ApiResult<ExecutionEndpointResult>> HandleAsync(GetExecutionEndpointQuery query, CancellationToken cancellationToken = default)
    {
        var endpoint = await _store.GetByIdAsync(query.EndpointId, cancellationToken);
        if (endpoint is null) return ApiResult<ExecutionEndpointResult>.Fail("Execution endpoint not found.", 404);
        if (!KioskAccessRules.CanAccessKiosk(query.UserContext, endpoint.Kiosk))
            return ApiResult<ExecutionEndpointResult>.Fail("Access denied.", 403);
        return ApiResult<ExecutionEndpointResult>.Success(ExecutionEndpointResultMapper.ToResult(endpoint));
    }
}
