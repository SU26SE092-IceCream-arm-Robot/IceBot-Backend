using Domain.Devices.ExecutionEndpoints;
using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;
using Domain.Common;
using Domain.Devices.Entities;

namespace Application.Devices.Commands;

public sealed class CreateExecutionEndpointCommandHandler
{
    private readonly IExecutionEndpointStore _store;
    public CreateExecutionEndpointCommandHandler(IExecutionEndpointStore store) => _store = store;

    public async Task<ApiResult<ExecutionEndpointResult>> HandleAsync(CreateExecutionEndpointCommand command, CancellationToken cancellationToken = default)
    {
        var kiosk = await _store.GetKioskByIdAsync(command.KioskId, cancellationToken);
        if (kiosk is null) return ApiResult<ExecutionEndpointResult>.Fail("Kiosk not found.", 404);
        if (!KioskAccessRules.CanAccessKiosk(command.UserContext, kiosk))
            return ApiResult<ExecutionEndpointResult>.Fail("Access denied.", 403);

        var code = command.Request.EndpointCode.Trim();
        if (await _store.EndpointCodeExistsAsync(kiosk.Id, code, cancellationToken))
            return ApiResult<ExecutionEndpointResult>.Fail("Execution endpoint code already exists in this kiosk.", 409);

        try
        {
            var authenticationMode = command.Request.ExecutionProfile == Domain.Devices.Enums.KioskExecutionProfile.FullEdge
                ? Domain.Devices.Enums.ExecutionEndpointAuthenticationMode.MutualTls
                : Domain.Devices.Enums.ExecutionEndpointAuthenticationMode.SignedCommandTls;
            var endpoint = KioskExecutionEndpoint.CreateProvisioning(
                kiosk.Id, code, command.Request.ExecutionProfile, authenticationMode);
            endpoint.CreatedByAccountId = command.UserContext.AccountId;
            await _store.AddAsync(endpoint, cancellationToken);
            await _store.SaveChangesAsync(cancellationToken);

            var created = await _store.GetByIdAsync(endpoint.Id, cancellationToken);
            return ApiResult<ExecutionEndpointResult>.Success(
                ExecutionEndpointResultMapper.ToResult(created!), "Execution endpoint created in provisioning state.", 201);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ExecutionEndpointResult>.Fail(ex.Message, 400);
        }
    }
}
