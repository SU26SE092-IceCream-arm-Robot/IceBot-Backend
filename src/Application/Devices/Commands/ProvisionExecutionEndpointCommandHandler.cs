using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Domain.Common;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Application.Devices.Security;

namespace Application.Devices.Commands;

public sealed class ProvisionExecutionEndpointCommandHandler
{
    private readonly IExecutionEndpointStore _store;
    public ProvisionExecutionEndpointCommandHandler(IExecutionEndpointStore store) => _store = store;

    public async Task<ApiResult<ExecutionEndpointResult>> HandleAsync(ProvisionExecutionEndpointCommand command, CancellationToken cancellationToken = default)
    {
        var loaded = await ExecutionEndpointCommandRules.LoadAccessibleAsync(_store, command.UserContext, command.EndpointId, cancellationToken);
        if (loaded.Error is not null) return loaded.Error;
        var endpoint = loaded.Endpoint!;
        if (endpoint.Status != KioskExecutionEndpointStatus.Provisioning)
            return ApiResult<ExecutionEndpointResult>.Fail("Only provisioning endpoints can be provisioned.", 400);
        if (endpoint.SupportedRobotTargets.Count == 0)
            return ApiResult<ExecutionEndpointResult>.Fail("At least one supported robot target is required before provisioning.", 400);
        if (command.Request.ProfileIdentity == Guid.Empty)
            return ApiResult<ExecutionEndpointResult>.Fail("Profile identity is required.", 400);
        if (await _store.ProfileIdentityExistsAsync(command.Request.ProfileIdentity, cancellationToken))
            return ApiResult<ExecutionEndpointResult>.Fail("Profile identity already belongs to another execution endpoint.", 409);

        ExecutionEndpointCredentialMaterial material;
        try
        {
            material = endpoint.AuthenticationMode == ExecutionEndpointAuthenticationMode.MutualTls
                ? ExecutionEndpointCredentialMaterialFactory.FromCertificateFingerprint(
                    command.Request.ClientCertificateSha256Fingerprint ?? string.Empty)
                : ExecutionEndpointCredentialMaterialFactory.FromEcdsaPublicKey(
                    command.Request.EcdsaPublicKeyPem ?? string.Empty);
        }
        catch (ArgumentException ex)
        {
            return ApiResult<ExecutionEndpointResult>.Fail(ex.Message, 400);
        }

        if (await _store.CredentialReferenceExistsAsync(material.Fingerprint, cancellationToken))
            return ApiResult<ExecutionEndpointResult>.Fail("Credential reference already exists.", 409);

        try
        {
            var now = DateTimeOffset.UtcNow;
            var credential = ExecutionEndpointCredentialBinding.CreateProvisioned(
                endpoint.Id, endpoint.AuthenticationMode, material.Fingerprint, now, material.PublicKeyPem);
            credential.CreatedByAccountId = command.UserContext.AccountId;
            await _store.AddCredentialBindingAsync(credential, cancellationToken);
            endpoint.AttachCredentialBinding(credential);
            endpoint.ActivateCredentialBinding(now);
            endpoint.Activate(command.Request.ProfileIdentity, now);
            endpoint.UpdatedByAccountId = command.UserContext.AccountId;
            await _store.SaveChangesAsync(cancellationToken);
            return ApiResult<ExecutionEndpointResult>.Success(
                ExecutionEndpointResultMapper.ToResult(endpoint), "Execution endpoint provisioned and activated successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ExecutionEndpointResult>.Fail(ex.Message, 400);
        }
    }
}
