using Application.Devices.Credentials.Abstractions;
using Application.Devices.Credentials.Support;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Domain.Devices.ExecutionEndpoints;

namespace Application.Devices.Credentials.Commands;

public sealed class ReconcileStaleMqttEndpointCredentialCommandHandler(
    IExecutionEndpointStore store,
    IMqttEndpointCredentialProvisioner provisioner)
{
    public async Task<MqttCredentialReconciliationOutcome> HandleAsync(
        ReconcileStaleMqttEndpointCredentialCommand command,
        CancellationToken cancellationToken = default)
    {
        PreparedRevocation? prepared = null;
        MqttCredentialReconciliationOutcome? immediateOutcome = null;
        var staleBefore = command.ObservedAt - MqttCredentialOperationPolicy.PendingOperationLease;
        var revocationClaimed = await store.ExecuteMqttCredentialMutationAsync(
            command.EndpointId,
            async ct =>
            {
                var endpoint = await store.GetByIdForCredentialRotationAsync(command.EndpointId, ct);
                var credential = endpoint?.MqttCredential;
                if (credential is null)
                {
                    immediateOutcome = MqttCredentialReconciliationOutcome.NotFound;
                    return false;
                }
                if ((credential.UpdatedAt ?? credential.CreatedAt) > staleBefore)
                {
                    immediateOutcome = MqttCredentialReconciliationOutcome.NoLongerStale;
                    return false;
                }

                switch (credential.Status)
                {
                    case ExecutionEndpointMqttCredentialStatus.PendingProvision:
                        credential.MarkFailed(
                            "MQTT provisioning did not complete before the operation lease expired. " +
                            "Retry provisioning to replace any broker credential whose secret was not returned.");
                        credential.UpdatedByAccountId = null;
                        await store.SaveChangesAsync(ct);
                        immediateOutcome = MqttCredentialReconciliationOutcome.ProvisioningMarkedFailed;
                        return false;

                    case ExecutionEndpointMqttCredentialStatus.PendingRotation:
                        credential.MarkFailed(
                            "MQTT rotation did not complete before the operation lease expired. " +
                            "Retry rotation to replace any broker credential whose secret was not returned.");
                        credential.UpdatedByAccountId = null;
                        await store.SaveChangesAsync(ct);
                        immediateOutcome = MqttCredentialReconciliationOutcome.RotationMarkedFailed;
                        return false;

                    case ExecutionEndpointMqttCredentialStatus.PendingRevoke:
                        credential.RestartPendingOperation();
                        break;

                    case ExecutionEndpointMqttCredentialStatus.RevokeFailed:
                        credential.BeginRevocation();
                        break;

                    default:
                        immediateOutcome = MqttCredentialReconciliationOutcome.NoLongerStale;
                        return false;
                }

                credential.UpdatedByAccountId = null;
                await store.SaveChangesAsync(ct);
                prepared = new PreparedRevocation(
                    credential.Username,
                    credential.CredentialVersion);
                return true;
            },
            cancellationToken);

        if (!revocationClaimed || prepared is null)
            return immediateOutcome ?? MqttCredentialReconciliationOutcome.NoLongerStale;

        try
        {
            await provisioner.RevokeAsync(
                command.EndpointId,
                prepared.Username,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await RecordRevocationFailureAsync(command.EndpointId, prepared, ex.Message);
            return MqttCredentialReconciliationOutcome.RevokeRetryFailed;
        }

        return await store.ExecuteMqttCredentialMutationAsync(
            command.EndpointId,
            async ct =>
            {
                var endpoint = await store.GetByIdForCredentialRotationAsync(command.EndpointId, ct);
                var credential = endpoint?.MqttCredential;
                if (!MatchesPreparedRevocation(credential, prepared))
                    return MqttCredentialReconciliationOutcome.Superseded;

                credential!.MarkRevoked(command.ObservedAt);
                credential.UpdatedByAccountId = null;
                await store.SaveChangesAsync(ct);
                return MqttCredentialReconciliationOutcome.Revoked;
            },
            cancellationToken);
    }

    private async Task RecordRevocationFailureAsync(
        Guid endpointId,
        PreparedRevocation prepared,
        string error)
    {
        try
        {
            await store.ExecuteMqttCredentialMutationAsync(
                endpointId,
                async ct =>
                {
                    var endpoint = await store.GetByIdForCredentialRotationAsync(endpointId, ct);
                    var credential = endpoint?.MqttCredential;
                    if (MatchesPreparedRevocation(credential, prepared))
                    {
                        credential!.MarkRevocationFailed(error);
                        credential.UpdatedByAccountId = null;
                        await store.SaveChangesAsync(ct);
                    }
                    return true;
                },
                CancellationToken.None);
        }
        catch
        {
            // The claimed PendingRevoke remains durable and can be retried after the lease.
        }
    }

    private static bool MatchesPreparedRevocation(
        ExecutionEndpointMqttCredential? credential,
        PreparedRevocation prepared) =>
        credential is not null &&
        credential.Status == ExecutionEndpointMqttCredentialStatus.PendingRevoke &&
        credential.CredentialVersion == prepared.CredentialVersion;

    private sealed record PreparedRevocation(string Username, int CredentialVersion);
}
