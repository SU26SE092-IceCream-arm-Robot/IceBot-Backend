using Domain.Devices.ExecutionEndpoints;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.ExecutionEndpoints.Results;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;
using Domain.Devices.Catalog;

namespace Application.Devices.ExecutionEndpoints.Commands;

internal static class ExecutionEndpointCommandRules
{
    public static async Task<(KioskExecutionEndpoint? Endpoint, ApiResult<ExecutionEndpointResult>? Error)> LoadAccessibleAsync(
        IExecutionEndpointStore store,
        CurrentUserContext userContext,
        Guid kioskId,
        Guid endpointId,
        CancellationToken cancellationToken)
    {
        var endpoint = await store.GetByKioskIdAsync(kioskId, endpointId, cancellationToken);
        if (endpoint is null)
            return (null, ApiResult<ExecutionEndpointResult>.Fail("Execution endpoint not found.", 404));
        if (!KioskAccessRules.CanAccessKiosk(userContext, endpoint.Kiosk))
            return (null, ApiResult<ExecutionEndpointResult>.Fail("Access denied.", 403));
        return (endpoint, null);
    }
}
