using Application.Devices.ExecutionEndpoints.Abstractions;
using Domain.Devices.ExecutionEndpoints;
using Application.Devices.Credentials.Abstractions;
using Application.Devices.Credentials.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Devices.Catalog;
using Application.Devices.Credentials.Security;

namespace Application.Devices.Credentials.Commands;

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

        var endpoint = await _executionEndpointStore.GetByKioskIdForCredentialRotationAsync(
            command.KioskId,
            command.EndpointId,
            cancellationToken);
        if (endpoint is null)
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail("Execution endpoint not found.", 404);
        }
        if (endpoint.KioskId != command.KioskId)
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail("Execution endpoint not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ExecutionEndpointsCredentialsManage, command.UserContext, endpoint.Kiosk.OrganizationId, endpoint.Kiosk.StoreId, endpoint.KioskId))
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail("Access denied.", 403);
        }

        ExecutionEndpointCredentialMaterial material;
        try
        {
            material = endpoint.AuthenticationMode == ExecutionEndpointAuthenticationMode.MutualTls
                ? ExecutionEndpointCredentialMaterialFactory.FromCertificateFingerprint(
                    command.ClientCertificateSha256Fingerprint ?? string.Empty)
                : ExecutionEndpointCredentialMaterialFactory.FromEcdsaPublicKey(
                    command.EcdsaPublicKeyPem ?? string.Empty);
        }
        catch (ArgumentException ex)
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail(ex.Message, 400);
        }

        if (await _executionEndpointStore.CredentialReferenceExistsAsync(material.Fingerprint, cancellationToken))
        {
            return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail("Credential reference already exists.", 409);
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            if (endpoint.Status is KioskExecutionEndpointStatus.Provisioning or KioskExecutionEndpointStatus.Retired)
            {
                return ApiResult<ExecutionEndpointCredentialRotationResult>.Fail(
                    "Only active or disabled execution endpoints can rotate credentials.", 400);
            }

            var newCredential = endpoint.RotateCredential(
                material.Fingerprint,
                now,
                material.PublicKeyPem);
            newCredential.CreatedByAccountId = command.UserContext.AccountId;

            await _executionEndpointStore.AddCredentialBindingAsync(newCredential, cancellationToken);
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
