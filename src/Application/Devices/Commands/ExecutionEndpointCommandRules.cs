using Application.Devices.Abstractions;
using Application.Devices.Results;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;
using Domain.Devices.Entities;

namespace Application.Devices.Commands;

internal static class ExecutionEndpointCommandRules
{
    public static async Task<(KioskExecutionEndpoint? Endpoint, ApiResult<ExecutionEndpointResult>? Error)> LoadAccessibleAsync(
        IExecutionEndpointStore store,
        CurrentUserContext userContext,
        Guid endpointId,
        CancellationToken cancellationToken)
    {
        var endpoint = await store.GetByIdAsync(endpointId, cancellationToken);
        if (endpoint is null)
            return (null, ApiResult<ExecutionEndpointResult>.Fail("Execution endpoint not found.", 404));
        if (!KioskAccessRules.CanAccessKiosk(userContext, endpoint.Kiosk))
            return (null, ApiResult<ExecutionEndpointResult>.Fail("Access denied.", 403));
        return (endpoint, null);
    }
}
