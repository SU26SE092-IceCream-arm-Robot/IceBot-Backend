using Application.Devices.ExecutionEndpoints.Abstractions;
using Domain.Devices.ExecutionEndpoints;
using System.Security.Cryptography;
using Application.Devices.Credentials.Abstractions;
using Application.Devices.Credentials.Results;
using Application.Devices.Credentials.Support;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Devices.Catalog;

namespace Application.Devices.Credentials.Commands;

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
            _store, _provisioner, command.KioskId, command.EndpointId, command.UserContext, rotate: false, cancellationToken);
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
            _store, _provisioner, command.KioskId, command.EndpointId, command.UserContext, rotate: true, cancellationToken);
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
        MqttEndpointCredentialWorkflow.RevokeAsync(
            _store, _provisioner, command.KioskId, command.EndpointId,
            command.UserContext, cancellationToken);
}

internal static class MqttEndpointCredentialWorkflow
{
    public static async Task<ApiResult<MqttEndpointCredentialResult>> ProvisionOrRotateAsync(
        IExecutionEndpointStore store,
        IMqttEndpointCredentialProvisioner provisioner,
        Guid kioskId,
        Guid endpointId,
        Application.Identity.Tokens.Claims.CurrentUserContext userContext,
        bool rotate,
        CancellationToken cancellationToken)
    {
        PreparedOperation? prepared = null;
        var preparation = await store.ExecuteMqttCredentialMutationAsync(endpointId, async ct =>
        {
            var endpoint = await store.GetByKioskIdForCredentialRotationAsync(kioskId, endpointId, ct);
            var accessError = ValidateAccess(endpoint, kioskId, userContext);
            if (accessError is not null)
                return ApiResult<object>.Fail(accessError.Value.Message, accessError.Value.StatusCode);
            if (endpoint!.Status == KioskExecutionEndpointStatus.Retired)
                return ApiResult<object>.Fail("Retired execution endpoints cannot use MQTT credentials.", 400);

            var credential = endpoint.MqttCredential;
            try
            {
                if (credential is null)
                {
                    if (rotate)
                        return ApiResult<object>.Fail(
                            "MQTT credential has not been provisioned.", 404);
                    credential = ExecutionEndpointMqttCredential.BeginProvision(endpoint.Id, provisioner.ProviderName);
                    credential.CreatedByAccountId = userContext.AccountId;
                    await store.AddMqttCredentialAsync(credential, ct);
                }
                else if (rotate)
                {
                    var retryError = PrepareRotation(credential, DateTimeOffset.UtcNow);
                    if (retryError is not null)
                        return ApiResult<object>.Fail(
                            retryError.Value.Message, retryError.Value.StatusCode);
                    credential.UpdatedByAccountId = userContext.AccountId;
                }
                else
                {
                    var retryError = PrepareProvision(credential, DateTimeOffset.UtcNow);
                    if (retryError is not null)
                        return ApiResult<object>.Fail(
                            retryError.Value.Message, retryError.Value.StatusCode);
                    credential.UpdatedByAccountId = userContext.AccountId;
                }
                await store.SaveChangesAsync(ct);
                prepared = new PreparedOperation(
                    endpoint.Id,
                    credential.Username,
                    GenerateSecret(),
                    credential.CredentialVersion,
                    credential.Status);
                return ApiResult<object>.Success(new { endpoint.Id });
            }
            catch (DomainRuleException ex)
            {
                return ApiResult<object>.Fail(ex.Message, 400);
            }
        }, cancellationToken);

        if (!preparation.Succeeded || prepared is null)
            return ApiResult<MqttEndpointCredentialResult>.Fail(
                preparation.Message ?? "MQTT credential operation could not be prepared.",
                preparation.StatusCode);

        try
        {
            await provisioner.ProvisionOrReplaceAsync(
                prepared.EndpointId, prepared.Username, prepared.Password, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await RecordProvisionFailureAsync(store, prepared, ex.Message);
            return ApiResult<MqttEndpointCredentialResult>.Fail(
                $"MQTT broker provisioning failed: {ex.Message}", 503);
        }

        return await store.ExecuteMqttCredentialMutationAsync(endpointId, async ct =>
        {
            var endpoint = await store.GetByKioskIdForCredentialRotationAsync(kioskId, endpointId, ct);
            var credential = endpoint?.MqttCredential;
            if (!MatchesPreparedOperation(credential, prepared))
                return ApiResult<MqttEndpointCredentialResult>.Fail(
                    "MQTT credential operation was superseded before activation.", 409);

            credential!.MarkActive(DateTimeOffset.UtcNow);
            credential.UpdatedByAccountId = userContext.AccountId;
            await store.SaveChangesAsync(ct);
            return ApiResult<MqttEndpointCredentialResult>.Success(new MqttEndpointCredentialResult
            {
                ExecutionEndpointId = endpointId,
                Username = prepared.Username,
                Password = prepared.Password,
                ClientId = prepared.Username,
                SubscribeTopic = provisioner.GetSubscribeTopic(endpointId),
                UplinkPublishTopicPattern = provisioner.GetUplinkPublishTopicPattern(endpointId),
                UplinkResultTopic = provisioner.GetUplinkResultTopic(endpointId),
                CredentialVersion = prepared.CredentialVersion,
                Status = credential.Status.ToString()
            }, rotate
                ? "MQTT credential rotated; the password is shown once."
                : "MQTT credential provisioned; the password is shown once.");
        }, cancellationToken);
    }

    public static async Task<ApiResult<object>> RevokeAsync(
        IExecutionEndpointStore store,
        IMqttEndpointCredentialProvisioner provisioner,
        Guid kioskId,
        Guid endpointId,
        Application.Identity.Tokens.Claims.CurrentUserContext userContext,
        CancellationToken cancellationToken)
    {
        PreparedOperation? prepared = null;
        var preparation = await store.ExecuteMqttCredentialMutationAsync(endpointId, async ct =>
        {
            var endpoint = await store.GetByKioskIdForCredentialRotationAsync(kioskId, endpointId, ct);
            var accessError = ValidateAccess(endpoint, kioskId, userContext);
            if (accessError is not null)
                return ApiResult<object>.Fail(accessError.Value.Message, accessError.Value.StatusCode);
            var credential = endpoint!.MqttCredential;
            if (credential is null)
                return ApiResult<object>.Fail("Active MQTT credential not found.", 404);
            if (credential.Status == ExecutionEndpointMqttCredentialStatus.Revoked)
                return ApiResult<object>.Success(new { endpoint.Id }, "MQTT credential is already revoked.");

            try
            {
                var retryError = PrepareRevocation(credential, DateTimeOffset.UtcNow);
                if (retryError is not null)
                    return ApiResult<object>.Fail(retryError.Value.Message, retryError.Value.StatusCode);
                credential.UpdatedByAccountId = userContext.AccountId;
                await store.SaveChangesAsync(ct);
                prepared = new PreparedOperation(
                    endpoint.Id,
                    credential.Username,
                    string.Empty,
                    credential.CredentialVersion,
                    credential.Status);
                return ApiResult<object>.Success(new { endpoint.Id });
            }
            catch (DomainRuleException ex)
            {
                return ApiResult<object>.Fail(ex.Message, 400);
            }
        }, cancellationToken);

        if (!preparation.Succeeded || prepared is null)
            return preparation;

        try
        {
            await provisioner.RevokeAsync(endpointId, prepared.Username, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await RecordRevocationFailureAsync(store, prepared, ex.Message);
            return ApiResult<object>.Fail(
                $"MQTT broker rejected credential revocation: {ex.Message}", 503);
        }

        return await store.ExecuteMqttCredentialMutationAsync(endpointId, async ct =>
        {
            var endpoint = await store.GetByKioskIdForCredentialRotationAsync(kioskId, endpointId, ct);
            var credential = endpoint?.MqttCredential;
            if (!MatchesPreparedOperation(credential, prepared))
                return ApiResult<object>.Fail(
                    "MQTT credential revocation was superseded before completion.", 409);
            credential!.MarkRevoked(DateTimeOffset.UtcNow);
            credential.UpdatedByAccountId = userContext.AccountId;
            await store.SaveChangesAsync(ct);
            return ApiResult<object>.Success(new { endpointId }, "MQTT credential revoked.");
        }, cancellationToken);
    }

    private static (string Message, int StatusCode)? PrepareProvision(
        ExecutionEndpointMqttCredential credential,
        DateTimeOffset now)
    {
        switch (credential.Status)
        {
            case ExecutionEndpointMqttCredentialStatus.Revoked:
                credential.BeginReprovision();
                return null;
            case ExecutionEndpointMqttCredentialStatus.Failed when !credential.ActivatedAt.HasValue:
                credential.RetryFailedProvisionOrRotation();
                return null;
            case ExecutionEndpointMqttCredentialStatus.PendingProvision when IsStale(credential, now):
                credential.RestartPendingOperation();
                return null;
            case ExecutionEndpointMqttCredentialStatus.PendingProvision:
                return ("MQTT credential provisioning is already in progress.", 409);
            case ExecutionEndpointMqttCredentialStatus.Active:
                return ("MQTT credential is already active; use rotate.", 409);
            default:
                return ("MQTT credential cannot be provisioned from its current state.", 409);
        }
    }

    private static (string Message, int StatusCode)? PrepareRotation(
        ExecutionEndpointMqttCredential credential,
        DateTimeOffset now)
    {
        switch (credential.Status)
        {
            case ExecutionEndpointMqttCredentialStatus.Active:
                credential.BeginRotation();
                return null;
            case ExecutionEndpointMqttCredentialStatus.Failed when credential.ActivatedAt.HasValue:
                credential.RetryFailedProvisionOrRotation();
                return null;
            case ExecutionEndpointMqttCredentialStatus.PendingRotation when IsStale(credential, now):
                credential.RestartPendingOperation();
                return null;
            case ExecutionEndpointMqttCredentialStatus.PendingRotation:
                return ("MQTT credential rotation is already in progress.", 409);
            default:
                return ("MQTT credential cannot be rotated from its current state.", 409);
        }
    }

    private static (string Message, int StatusCode)? PrepareRevocation(
        ExecutionEndpointMqttCredential credential,
        DateTimeOffset now)
    {
        switch (credential.Status)
        {
            case ExecutionEndpointMqttCredentialStatus.Active:
            case ExecutionEndpointMqttCredentialStatus.Failed:
            case ExecutionEndpointMqttCredentialStatus.RevokeFailed:
                credential.BeginRevocation();
                return null;
            case ExecutionEndpointMqttCredentialStatus.PendingRevoke when IsStale(credential, now):
                credential.RestartPendingOperation();
                return null;
            case ExecutionEndpointMqttCredentialStatus.PendingRevoke:
                return ("MQTT credential revocation is already in progress.", 409);
            default:
                return ("MQTT credential cannot be revoked while another operation is pending.", 409);
        }
    }

    private static bool IsStale(ExecutionEndpointMqttCredential credential, DateTimeOffset now) =>
        (credential.UpdatedAt ?? credential.CreatedAt) <=
        now - MqttCredentialOperationPolicy.PendingOperationLease;

    private static bool MatchesPreparedOperation(
        ExecutionEndpointMqttCredential? credential,
        PreparedOperation prepared) =>
        credential is not null &&
        credential.CredentialVersion == prepared.CredentialVersion &&
        credential.Status == prepared.PendingStatus;

    private static async Task RecordProvisionFailureAsync(
        IExecutionEndpointStore store,
        PreparedOperation prepared,
        string error)
    {
        try
        {
            await store.ExecuteMqttCredentialMutationAsync(prepared.EndpointId, async ct =>
            {
                var endpoint = await store.GetByIdForCredentialRotationAsync(prepared.EndpointId, ct);
                var credential = endpoint?.MqttCredential;
                if (MatchesPreparedOperation(credential, prepared))
                {
                    credential!.MarkFailed(error);
                    await store.SaveChangesAsync(ct);
                }
                return true;
            }, CancellationToken.None);
        }
        catch
        {
            // The durable pending state can be reclaimed after the operation lease.
        }
    }

    private static async Task RecordRevocationFailureAsync(
        IExecutionEndpointStore store,
        PreparedOperation prepared,
        string error)
    {
        try
        {
            await store.ExecuteMqttCredentialMutationAsync(prepared.EndpointId, async ct =>
            {
                var endpoint = await store.GetByIdForCredentialRotationAsync(prepared.EndpointId, ct);
                var credential = endpoint?.MqttCredential;
                if (MatchesPreparedOperation(credential, prepared))
                {
                    credential!.MarkRevocationFailed(error);
                    await store.SaveChangesAsync(ct);
                }
                return true;
            }, CancellationToken.None);
        }
        catch
        {
            // The durable pending state can be reclaimed after the operation lease.
        }
    }

    internal static (string Message, int StatusCode)? ValidateAccess(
        KioskExecutionEndpoint? endpoint,
        Guid kioskId,
        Application.Identity.Tokens.Claims.CurrentUserContext userContext)
    {
        if (endpoint is null) return ("Execution endpoint not found.", 404);
        if (endpoint.KioskId != kioskId) return ("Execution endpoint not found.", 404);
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

    private sealed record PreparedOperation(
        Guid EndpointId,
        string Username,
        string Password,
        int CredentialVersion,
        ExecutionEndpointMqttCredentialStatus PendingStatus);
}
