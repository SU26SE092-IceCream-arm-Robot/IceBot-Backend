using Application.Devices.Abstractions;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Devices.Entities;

namespace Application.Devices.Commands;

public sealed class RotateExecutionEndpointCredentialCommandHandler
{
    private readonly IExecutionEndpointStore _executionEndpointStore;

    public RotateExecutionEndpointCredentialCommandHandler(IExecutionEndpointStore executionEndpointStore)
    {
        _executionEndpointStore = executionEndpointStore;
    }

    public async Task<ApiResult<ExecutionEndpointCredentialRotationResult>> HandleAsync(
        RotateExecutionEndpointCredentialCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.EndpointId == Guid.Empty)
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail("Execution endpoint is required.", 400);
        }

        if (string.IsNullOrWhiteSpace(command.CredentialReference))
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail("Credential reference is required.", 400);
        }

        var endpoint = await _executionEndpointStore.GetByIdForCredentialRotationAsync(command.EndpointId, cancellationToken);
        if (endpoint is null)
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail("Execution endpoint not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(command.UserContext, endpoint.Kiosk.OrganizationId, endpoint.Kiosk.StoreId, endpoint.KioskId))
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail("Access denied.", 403);
        }

        var credentialReference = command.CredentialReference.Trim();
        if (await _executionEndpointStore.CredentialReferenceExistsAsync(credentialReference, cancellationToken))
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail("Credential reference already exists.", 409);
        }

        if (command.AuthenticationMode.HasValue && command.AuthenticationMode.Value != endpoint.AuthenticationMode)
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail("Credential authentication mode does not match the endpoint.", 400);
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            if (endpoint.CredentialBinding is not null)
            {
                endpoint.RevokeCredential(now);
            }

            var newCredential = ExecutionEndpointCredentialBinding.CreateProvisioned(
                endpoint.Id,
                endpoint.AuthenticationMode,
                credentialReference,
                now);
            newCredential.CreatedByAccountId = command.UserContext.AccountId;

            await _executionEndpointStore.AddCredentialBindingAsync(newCredential, cancellationToken);

            endpoint.AttachCredentialBinding(newCredential);
            endpoint.ActivateCredentialBinding(now);
            endpoint.ReactivateWithCurrentCredential(now);
            endpoint.UpdatedByAccountId = command.UserContext.AccountId;

            await _executionEndpointStore.SaveChangesAsync(cancellationToken);

            return ApiResult<ExecutionEndpointCredentialRotationResult>.Success(
                ExecutionEndpointCredentialRotationResult.FromEndpoint(endpoint),
                "Execution endpoint credential rotated successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail(ex.Message, 400);
        }
    }
}
