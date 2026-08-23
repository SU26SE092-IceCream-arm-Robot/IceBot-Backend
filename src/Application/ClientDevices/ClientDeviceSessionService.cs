using Application.ClientDevices.Abstractions;
using Application.ClientDevices.Contracts;
using Application.ClientDevices.Security;
using Application.Shared.Wrappers;
using Domain.Devices.ClientDevices;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Application.ClientDevices;

public sealed class ClientDeviceSessionService(
    IClientDeviceStore store,
    ClientDeviceCredentialHasher credentialHasher,
    IClientDeviceTokenIssuer tokenIssuer,
    IOptions<ClientDeviceSecurityOptions> options,
    ILogger<ClientDeviceSessionService> logger)
{
    private static readonly Meter Meter = new("IceBot.ClientDevices");
    private static readonly Counter<long> SessionAuthenticationFailures =
        Meter.CreateCounter<long>("icebot.client_devices.session_authentication_failures");
    private readonly ClientDeviceSecurityOptions _options = options.Value;

    public async Task<ApiResult<ClientDeviceSessionResult>> CreateAsync(
        CreateClientDeviceSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ClientDeviceId == Guid.Empty || request.InstallationId == Guid.Empty ||
            !ClientDeviceCredentialHasher.TryDecodeCredential(request.Credential, out _))
            return Reject("invalid_request", 400);

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            await store.AcquireClientDeviceLockAsync(request.ClientDeviceId, ct);
            var device = await store.GetByIdAsync(request.ClientDeviceId, tracking: true, ct);
            if (device is null || device.Type != ClientDeviceType.SelfOrderTablet ||
                device.Status != ClientDeviceStatus.Active || device.InstallationId != request.InstallationId)
                return Reject("device_state", 401);

            var credential = device.Credentials.SingleOrDefault(value => value.Status == ClientDeviceCredentialStatus.Active);
            if (credential is null || credential.Version != device.CredentialVersion ||
                !credentialHasher.Matches(request.Credential, credential.SecretHash, credential.HashKeyVersion))
                return Reject("credential", 401);

            var now = DateTimeOffset.UtcNow;
            if (device.TryObserve(now, TimeSpan.FromMinutes(_options.LastSeenMinimumIntervalMinutes)))
                await store.SaveChangesAsync(ct);

            var expiresAt = now.AddMinutes(_options.TokenLifetimeMinutes);
            return ApiResult<ClientDeviceSessionResult>.Success(new ClientDeviceSessionResult(
                tokenIssuer.Issue(device.Id, device.KioskId, device.CredentialVersion, device.SessionVersion),
                expiresAt,
                ClientDeviceResultMapper.ToResult(device)));
        }, cancellationToken);
    }

    private ApiResult<ClientDeviceSessionResult> Reject(string reason, int statusCode)
    {
        SessionAuthenticationFailures.Add(1, new KeyValuePair<string, object?>("reason", reason));
        logger.LogWarning("Client-device session exchange was rejected: {Reason}.", reason);
        return ApiResult<ClientDeviceSessionResult>.Fail(
            statusCode == 400
                ? "A valid client device id, installation id, and credential are required."
                : "Client device authentication failed.",
            statusCode);
    }
}
