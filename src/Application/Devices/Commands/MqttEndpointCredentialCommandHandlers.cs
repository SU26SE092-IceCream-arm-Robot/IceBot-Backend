using Domain.Devices.ExecutionEndpoints;
using System.Security.Cryptography;
using Application.Devices.Abstractions;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Devices.Entities;
using Domain.Devices.Enums;

namespace Application.Devices.Commands;

public sealed class ProvisionMqttEndpointCredentialCommandHandler
{
    private readonly IExecutionEndpointStore _store;
    private readonly IMqttEndpointCredentialProvisioner _provisioner;

    public ProvisionMqttEndpointCredentialCommandHandler(IExecutionEndpointStore store, IMqttEndpointCredentialProvisioner provisioner)
    {
        _store = store;
        _provisioner = provisioner;
    }

    public Task<ApiResult<MqttEndpointCredentialResult>> HandleAsync(
        ProvisionMqttEndpointCredentialCommand command,
        CancellationToken cancellationToken = default) =>
        MqttEndpointCredentialWorkflow.ProvisionOrRotateAsync(
            _store, _provisioner, command.EndpointId, command.UserContext, rotate: false, cancellationToken);
}

public sealed class RotateMqttEndpointCredentialCommandHandler
{
    private readonly IExecutionEndpointStore _store;
    private readonly IMqttEndpointCredentialProvisioner _provisioner;

    public RotateMqttEndpointCredentialCommandHandler(IExecutionEndpointStore store, IMqttEndpointCredentialProvisioner provisioner)
    {
        _store = store;
        _provisioner = provisioner;
    }

    public Task<ApiResult<MqttEndpointCredentialResult>> HandleAsync(
        RotateMqttEndpointCredentialCommand command,
        CancellationToken cancellationToken = default) =>
        MqttEndpointCredentialWorkflow.ProvisionOrRotateAsync(
            _store, _provisioner, command.EndpointId, command.UserContext, rotate: true, cancellationToken);
}

public sealed class RevokeMqttEndpointCredentialCommandHandler
{
    private readonly IExecutionEndpointStore _store;
    private readonly IMqttEndpointCredentialProvisioner _provisioner;

    public RevokeMqttEndpointCredentialCommandHandler(IExecutionEndpointStore store, IMqttEndpointCredentialProvisioner provisioner)
    {
        _store = store;
        _provisioner = provisioner;
    }

    public Task<ApiResult<object>> HandleAsync(RevokeMqttEndpointCredentialCommand command, CancellationToken cancellationToken = default) =>
        _store.ExecuteMqttCredentialMutationAsync(command.EndpointId, async ct =>
        {
            var endpoint = await _store.GetByIdForCredentialRotationAsync(command.EndpointId, ct);
            var accessError = MqttEndpointCredentialWorkflow.ValidateAccess(endpoint, command.UserContext);
            if (accessError is not null) return ApiResult<object>.Fail(accessError.Value.Message, accessError.Value.StatusCode);
            if (endpoint!.MqttCredential is null || endpoint.MqttCredential.Status == ExecutionEndpointMqttCredentialStatus.Revoked)
                return ApiResult<object>.Fail("Active MQTT credential not found.", 404);

            try
            {
                await _provisioner.RevokeAsync(endpoint.Id, endpoint.MqttCredential.Username, ct);
                endpoint.MqttCredential.MarkRevoked(DateTimeOffset.UtcNow);
                endpoint.MqttCredential.UpdatedByAccountId = command.UserContext.AccountId;
                await _store.SaveChangesAsync(ct);
                return ApiResult<object>.Success(new { endpoint.Id }, "MQTT credential revoked.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ApiResult<object>.Fail($"MQTT broker rejected credential revocation: {ex.Message}", 503);
            }
        }, cancellationToken);
}

internal static class MqttEndpointCredentialWorkflow
{
    public static Task<ApiResult<MqttEndpointCredentialResult>> ProvisionOrRotateAsync(
        IExecutionEndpointStore store,
        IMqttEndpointCredentialProvisioner provisioner,
        Guid endpointId,
        Application.Identity.Tokens.Claims.CurrentUserContext userContext,
        bool rotate,
        CancellationToken cancellationToken) =>
        store.ExecuteMqttCredentialMutationAsync(endpointId, async ct =>
        {
            var endpoint = await store.GetByIdForCredentialRotationAsync(endpointId, ct);
            var accessError = ValidateAccess(endpoint, userContext);
            if (accessError is not null)
                return ApiResult<MqttEndpointCredentialResult>.Fail(accessError.Value.Message, accessError.Value.StatusCode);
            if (endpoint!.Status == KioskExecutionEndpointStatus.Retired)
                return ApiResult<MqttEndpointCredentialResult>.Fail("Retired execution endpoints cannot use MQTT credentials.", 400);

            var credential = endpoint.MqttCredential;
            if (!rotate && credential?.Status == ExecutionEndpointMqttCredentialStatus.Active)
                return ApiResult<MqttEndpointCredentialResult>.Fail("MQTT credential is already active; use rotate.", 409);
            if (rotate && credential is null)
                return ApiResult<MqttEndpointCredentialResult>.Fail("MQTT credential has not been provisioned.", 404);

            try
            {
                if (credential is null)
                {
                    credential = ExecutionEndpointMqttCredential.BeginProvision(endpoint.Id, provisioner.ProviderName);
                    credential.CreatedByAccountId = userContext.AccountId;
                    await store.AddMqttCredentialAsync(credential, ct);
                }
                else
                {
                    credential.BeginRotation();
                    credential.UpdatedByAccountId = userContext.AccountId;
                }
                await store.SaveChangesAsync(ct);

                var password = GenerateSecret();
                await provisioner.ProvisionOrReplaceAsync(endpoint.Id, credential.Username, password, ct);
                credential.MarkActive(DateTimeOffset.UtcNow);
                await store.SaveChangesAsync(ct);
                var topic = provisioner.GetSubscribeTopic(endpoint.Id);
                return ApiResult<MqttEndpointCredentialResult>.Success(new MqttEndpointCredentialResult
                {
                    ExecutionEndpointId = endpoint.Id,
                    Username = credential.Username,
                    Password = password,
                    ClientId = credential.Username,
                    SubscribeTopic = topic,
                    CredentialVersion = credential.CredentialVersion,
                    Status = credential.Status.ToString()
                }, rotate ? "MQTT credential rotated; the password is shown once." : "MQTT credential provisioned; the password is shown once.");
            }
            catch (DomainRuleException ex)
            {
                return ApiResult<MqttEndpointCredentialResult>.Fail(ex.Message, 400);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (credential is not null && credential.Status is ExecutionEndpointMqttCredentialStatus.PendingProvision or ExecutionEndpointMqttCredentialStatus.PendingRotation)
                {
                    credential.MarkFailed(ex.Message);
                    await store.SaveChangesAsync(ct);
                }
                return ApiResult<MqttEndpointCredentialResult>.Fail($"MQTT broker provisioning failed: {ex.Message}", 503);
            }
        }, cancellationToken);

    internal static (string Message, int StatusCode)? ValidateAccess(
        KioskExecutionEndpoint? endpoint,
        Application.Identity.Tokens.Claims.CurrentUserContext userContext)
    {
        if (endpoint is null) return ("Execution endpoint not found.", 404);
        return ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.DevicesManage, userContext, endpoint.Kiosk.OrganizationId, endpoint.Kiosk.StoreId, endpoint.KioskId)
            ? null
            : ("Access denied.", 403);
    }

    private static string GenerateSecret()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
