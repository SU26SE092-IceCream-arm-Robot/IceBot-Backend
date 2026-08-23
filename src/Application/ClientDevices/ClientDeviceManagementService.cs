using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.ClientDevices.Abstractions;
using Application.ClientDevices.Contracts;
using Application.ClientDevices.Security;
using Application.Identity.Tokens.Claims;
using Application.Shared.Idempotency;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common.Enums;
using Domain.Devices.ClientDevices;
using Domain.Operations.Entities;

namespace Application.ClientDevices;

public sealed class ClientDeviceManagementService(
    IClientDeviceStore store,
    ClientDeviceCredentialHasher credentialHasher)
{
    public async Task<ApiResult<IReadOnlyList<ClientDeviceResult>>> ListAsync(
        Guid kioskId,
        CurrentUserContext user,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await store.GetKioskAsync(kioskId, cancellationToken);
        if (kiosk is null || !CanAccess(ScopeRoleSets.ClientDevicesView, user, kiosk))
            return ApiResult<IReadOnlyList<ClientDeviceResult>>.Fail("Client device not found.", 404);

        var devices = await store.ListByKioskAsync(kioskId, cancellationToken);
        return ApiResult<IReadOnlyList<ClientDeviceResult>>.Success(devices.Select(ClientDeviceResultMapper.ToResult).ToArray());
    }

    public async Task<ApiResult<ClientDeviceResult>> GetAsync(
        Guid clientDeviceId,
        CurrentUserContext user,
        CancellationToken cancellationToken = default)
    {
        var device = await store.GetByIdAsync(clientDeviceId, tracking: false, cancellationToken);
        if (device is null || !CanAccess(ScopeRoleSets.ClientDevicesView, user, device))
            return ApiResult<ClientDeviceResult>.Fail("Client device not found.", 404);

        return ApiResult<ClientDeviceResult>.Success(ClientDeviceResultMapper.ToResult(device));
    }

    public Task<ApiResult<ClientDeviceResult>> ProvisionAsync(
        Guid kioskId,
        ProvisionClientDeviceRequest request,
        string? idempotencyKey,
        CurrentUserContext user,
        CancellationToken cancellationToken = default) =>
        ExecuteMutationAsync(
            kioskId,
            operation: "Provision",
            idempotencyKey,
            request.Reason,
            user,
            ScopeRoleSets.ClientDevicesProvision,
            requestFingerprint: Fingerprint(
                request.InstallationId,
                request.DisplayName,
                request.AppVersion,
                request.Platform,
                request.Reason,
                CredentialFingerprint(request.Credential)),
            async (kiosk, now, actorId, ct) =>
            {
                if (!ClientDeviceCredentialHasher.TryDecodeCredential(request.Credential, out _))
                    return ApiResult<ClientDeviceResult>.Fail("Credential must be a base64-encoded 256-bit secret.", 400);
                if (request.InstallationId == Guid.Empty || string.IsNullOrWhiteSpace(request.DisplayName))
                    return ApiResult<ClientDeviceResult>.Fail("Installation id and display name are required.", 400);

                var existingInstallation = await store.GetByInstallationIdAsync(request.InstallationId, tracking: true, ct);
                if (existingInstallation is not null)
                {
                    return ApiResult<ClientDeviceResult>.Fail(
                        "Installation id is already bound to a non-retired client device.", 409);
                }

                var existingDevice = (await store.ListByKioskAsync(kiosk.Id, ct))
                    .SingleOrDefault(device => device.Type == ClientDeviceType.SelfOrderTablet && device.Status != ClientDeviceStatus.Retired);
                if (existingDevice is not null)
                    return ApiResult<ClientDeviceResult>.Fail("Kiosk already has an active tablet binding.", 409);

                var device = ClientDevice.Provision(
                    kiosk,
                    ClientDeviceType.SelfOrderTablet,
                    request.InstallationId,
                    request.DisplayName,
                    request.AppVersion,
                    request.Platform,
                    now,
                    actorId);
                device.AddInitialCredential(credentialHasher.ComputeCurrent(request.Credential), credentialHasher.CurrentHashKeyVersion, now, actorId);
                await store.AddClientDeviceAsync(device, ct);
                return ApiResult<ClientDeviceResult>.Success(ClientDeviceResultMapper.ToResult(device), "Client device provisioned.", 201);
            },
            cancellationToken);

    public Task<ApiResult<ClientDeviceResult>> DisableAsync(
        Guid clientDeviceId,
        ClientDeviceLifecycleRequest request,
        string? idempotencyKey,
        CurrentUserContext user,
        CancellationToken cancellationToken = default) =>
        ExecuteExistingMutationAsync(
            clientDeviceId,
            operation: "Disable",
            idempotencyKey,
            request.Reason,
            user,
            ScopeRoleSets.ClientDevicesOperationsManage,
            request.ExpectedRevision,
            Fingerprint(request.ExpectedRevision, request.Reason),
            (device, _, actorId, now, _) =>
            {
                device.Disable(now, actorId);
                return Task.FromResult(ApiResult<ClientDeviceResult>.Success(ClientDeviceResultMapper.ToResult(device), "Client device disabled."));
            },
            cancellationToken);

    public Task<ApiResult<ClientDeviceResult>> ReactivateAsync(
        Guid clientDeviceId,
        ClientDeviceLifecycleRequest request,
        string? idempotencyKey,
        CurrentUserContext user,
        CancellationToken cancellationToken = default) =>
        ExecuteExistingMutationAsync(
            clientDeviceId,
            operation: "Reactivate",
            idempotencyKey,
            request.Reason,
            user,
            ScopeRoleSets.ClientDevicesOperationsManage,
            request.ExpectedRevision,
            Fingerprint(request.ExpectedRevision, request.Reason),
            (device, _, actorId, now, _) =>
            {
                device.Reactivate(now, actorId);
                return Task.FromResult(ApiResult<ClientDeviceResult>.Success(ClientDeviceResultMapper.ToResult(device), "Client device reactivated."));
            },
            cancellationToken);

    public Task<ApiResult<ClientDeviceResult>> RotateCredentialAsync(
        Guid clientDeviceId,
        RotateClientDeviceCredentialRequest request,
        string? idempotencyKey,
        CurrentUserContext user,
        CancellationToken cancellationToken = default) =>
        ExecuteExistingMutationAsync(
            clientDeviceId,
            operation: "RotateCredential",
            idempotencyKey,
            request.Reason,
            user,
            ScopeRoleSets.ClientDevicesCredentialsManage,
            request.ExpectedRevision,
            Fingerprint(request.ExpectedRevision, request.Reason, CredentialFingerprint(request.Credential)),
            (device, _, actorId, now, _) =>
            {
                if (!ClientDeviceCredentialHasher.TryDecodeCredential(request.Credential, out _))
                    return Task.FromResult(ApiResult<ClientDeviceResult>.Fail("Credential must be a base64-encoded 256-bit secret.", 400));
                device.RotateCredential(credentialHasher.ComputeCurrent(request.Credential), credentialHasher.CurrentHashKeyVersion, now, actorId);
                return Task.FromResult(ApiResult<ClientDeviceResult>.Success(ClientDeviceResultMapper.ToResult(device), "Client device credential rotated."));
            },
            cancellationToken);

    public async Task<ApiResult<ClientDeviceResult>> RetireAsync(
        Guid clientDeviceId,
        ClientDeviceLifecycleRequest request,
        string? idempotencyKey,
        CurrentUserContext user,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteExistingMutationAsync(
            clientDeviceId,
            operation: "Retire",
            idempotencyKey,
            request.Reason,
            user,
            ScopeRoleSets.ClientDevicesRebind,
            request.ExpectedRevision,
            Fingerprint(request.ExpectedRevision, request.Reason),
            async (device, kiosk, actorId, now, ct) =>
            {
                if (await store.HasActiveCustomerSessionAsync(kiosk.Id, now, ct))
                    return ApiResult<ClientDeviceResult>.Fail("Client device cannot be retired while the kiosk has an active customer session.", 409);
                device.Retire(now, actorId);
                return ApiResult<ClientDeviceResult>.Success(ClientDeviceResultMapper.ToResult(device), "Client device retired.");
            },
            cancellationToken);
    }

    public async Task<ApiResult<ClientDeviceResult>> RebindAsync(
        Guid clientDeviceId,
        RebindClientDeviceRequest request,
        string? idempotencyKey,
        CurrentUserContext user,
        CancellationToken cancellationToken = default)
    {
        if (request.TargetKioskId == Guid.Empty)
            return ApiResult<ClientDeviceResult>.Fail("Target kiosk is required.", 400);

        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var observedDevice = await store.GetByIdAsync(clientDeviceId, tracking: false, ct);
            if (observedDevice is null)
                return ApiResult<ClientDeviceResult>.Fail("Client device not found.", 404);
            var sourceKiosk = await store.GetKioskAsync(observedDevice.KioskId, ct);
            var targetKiosk = await store.GetKioskAsync(request.TargetKioskId, ct);
            if (sourceKiosk is null || targetKiosk is null ||
                !CanAccess(ScopeRoleSets.ClientDevicesRebind, user, sourceKiosk) ||
                !CanAccess(ScopeRoleSets.ClientDevicesRebind, user, targetKiosk))
                return ApiResult<ClientDeviceResult>.Fail("Client device not found.", 404);
            if (!TryNormalizeMutation(request.Reason, idempotencyKey, out var normalizedReason, out var normalizedKey, out var error))
                return ApiResult<ClientDeviceResult>.Fail(error!, 400);
            foreach (var kioskId in new[] { sourceKiosk.Id, targetKiosk.Id }.Distinct().OrderBy(value => value))
                await store.AcquireKioskLockAsync(kioskId, ct);
            await store.AcquireClientDeviceLockAsync(clientDeviceId, ct);

            var device = await store.GetByIdAsync(clientDeviceId, tracking: true, ct);
            if (device is null || device.KioskId != sourceKiosk.Id)
                return ApiResult<ClientDeviceResult>.Fail("Client device observation is stale.", 409);

            var fingerprint = Fingerprint(request.TargetKioskId, request.ExpectedRevision, normalizedReason);
            var replay = await store.GetReplayForClientDeviceAsync(device.Id, "Rebind", normalizedKey!, ct);
            if (replay is not null)
                return await ReplayResultAsync(replay, fingerprint, ct);

            if (device.Revision != request.ExpectedRevision)
                return ApiResult<ClientDeviceResult>.Fail("Client device revision is stale.", 409);

            if (await store.HasActiveCustomerSessionAsync(sourceKiosk.Id, DateTimeOffset.UtcNow, ct) ||
                await store.HasActiveCustomerSessionAsync(targetKiosk.Id, DateTimeOffset.UtcNow, ct))
                return ApiResult<ClientDeviceResult>.Fail("Client device cannot be rebound while either kiosk has an active customer session.", 409);

            var targetExisting = (await store.ListByKioskAsync(targetKiosk.Id, ct))
                .SingleOrDefault(candidate => candidate.Type == ClientDeviceType.SelfOrderTablet && candidate.Status != ClientDeviceStatus.Retired);
            if (targetExisting is not null && targetExisting.Id != device.Id)
                return ApiResult<ClientDeviceResult>.Fail("Target kiosk already has an active tablet binding.", 409);

            var oldScope = new { device.OrganizationId, device.StoreId, device.KioskId };
            var now = DateTimeOffset.UtcNow;
            device.Rebind(targetKiosk, now, user.AccountId);
            var result = ApiResult<ClientDeviceResult>.Success(ClientDeviceResultMapper.ToResult(device), "Client device rebound.");
            await PersistMutationAsync(sourceKiosk.Id, device, "Rebind", normalizedKey!, fingerprint, normalizedReason!, oldScope, now, user.AccountId, ct);
            return result;
        }, cancellationToken);
    }

    public async Task<ApiResult<ClientDeviceResult>> ReplaceAsync(
        Guid kioskId,
        ReplaceClientDeviceRequest request,
        string? idempotencyKey,
        CurrentUserContext user,
        CancellationToken cancellationToken = default)
    {
        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var kiosk = await store.GetKioskAsync(kioskId, ct);
            if (kiosk is null || !CanAccess(ScopeRoleSets.ClientDevicesRebind, user, kiosk))
                return ApiResult<ClientDeviceResult>.Fail("Kiosk not found.", 404);
            if (!TryNormalizeMutation(request.Reason, idempotencyKey, out var normalizedReason, out var normalizedKey, out var error))
                return ApiResult<ClientDeviceResult>.Fail(error!, 400);
            if (request.ExpectedCurrentClientDeviceId == Guid.Empty || request.ReplacementInstallationId == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.DisplayName) || !ClientDeviceCredentialHasher.TryDecodeCredential(request.Credential, out _))
                return ApiResult<ClientDeviceResult>.Fail("A current device, replacement installation, display name, and 256-bit credential are required.", 400);

            await store.AcquireKioskLockAsync(kiosk.Id, ct);
            await store.AcquireClientDeviceLockAsync(request.ExpectedCurrentClientDeviceId, ct);
            var fingerprint = Fingerprint(request.ExpectedCurrentClientDeviceId, request.ExpectedCurrentRevision,
                request.ReplacementInstallationId, request.DisplayName, request.AppVersion, request.Platform,
                normalizedReason, CredentialFingerprint(request.Credential));
            var replay = await store.GetReplayForClientDeviceAsync(request.ExpectedCurrentClientDeviceId, "Replace", normalizedKey!, ct);
            if (replay is not null)
                return await ReplayResultAsync(replay, fingerprint, ct);

            var current = await store.GetByIdAsync(request.ExpectedCurrentClientDeviceId, tracking: true, ct);
            if (current is null || current.KioskId != kiosk.Id || current.Revision != request.ExpectedCurrentRevision ||
                current.Type != ClientDeviceType.SelfOrderTablet || current.Status == ClientDeviceStatus.Retired)
                return ApiResult<ClientDeviceResult>.Fail("Current client device observation is stale.", 409);
            if (await store.HasActiveCustomerSessionAsync(kiosk.Id, DateTimeOffset.UtcNow, ct))
                return ApiResult<ClientDeviceResult>.Fail("Client device cannot be replaced while the kiosk has an active customer session.", 409);
            if (await store.GetByInstallationIdAsync(request.ReplacementInstallationId, tracking: true, ct) is not null)
                return ApiResult<ClientDeviceResult>.Fail("Replacement installation id is already bound to a non-retired client device.", 409);

            var now = DateTimeOffset.UtcNow;
            current.Retire(now, user.AccountId);
            var replacement = ClientDevice.Provision(kiosk, ClientDeviceType.SelfOrderTablet, request.ReplacementInstallationId,
                request.DisplayName, request.AppVersion, request.Platform, now, user.AccountId);
            replacement.AddInitialCredential(credentialHasher.ComputeCurrent(request.Credential), credentialHasher.CurrentHashKeyVersion, now, user.AccountId);
            await store.AddClientDeviceAsync(replacement, ct);
            var result = ApiResult<ClientDeviceResult>.Success(ClientDeviceResultMapper.ToResult(replacement), "Client device replaced.", 201);
            await PersistMutationAsync(kiosk.Id, replacement, "Replace", normalizedKey!, fingerprint, normalizedReason!,
                new { RetiredClientDeviceId = current.Id, RetiredRevision = current.Revision }, now, user.AccountId, ct,
                replayClientDeviceId: current.Id);
            return result;
        }, cancellationToken);
    }

    private async Task<ApiResult<ClientDeviceResult>> ExecuteExistingMutationAsync(
        Guid clientDeviceId,
        string operation,
        string? idempotencyKey,
        string reason,
        CurrentUserContext user,
        IReadOnlyCollection<string> allowedRoles,
        int expectedRevision,
        string requestFingerprint,
        Func<ClientDevice, Domain.Tenants.Entities.Kiosk, Guid, DateTimeOffset, CancellationToken, Task<ApiResult<ClientDeviceResult>>> action,
        CancellationToken cancellationToken)
    {
        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var device = await store.GetByIdAsync(clientDeviceId, tracking: true, ct);
            if (device is null)
                return ApiResult<ClientDeviceResult>.Fail("Client device not found.", 404);
            var kiosk = await store.GetKioskAsync(device.KioskId, ct);
            if (kiosk is null || !CanAccess(allowedRoles, user, kiosk))
                return ApiResult<ClientDeviceResult>.Fail("Client device not found.", 404);
            if (!TryNormalizeMutation(reason, idempotencyKey, out var normalizedReason, out var normalizedKey, out var error))
                return ApiResult<ClientDeviceResult>.Fail(error!, 400);

            await store.AcquireKioskLockAsync(kiosk.Id, ct);
            await store.AcquireClientDeviceLockAsync(device.Id, ct);
            var replay = await store.GetReplayAsync(kiosk.Id, operation, normalizedKey!, ct);
            if (replay is not null)
                return await ReplayResultAsync(replay, requestFingerprint, ct);
            if (device.Revision != expectedRevision)
                return ApiResult<ClientDeviceResult>.Fail("Client device revision is stale.", 409);

            var oldScope = new { device.OrganizationId, device.StoreId, device.KioskId, device.Status, device.Revision };
            var now = DateTimeOffset.UtcNow;
            var result = await action(device, kiosk, user.AccountId, now, ct);
            if (!result.Succeeded || result.Data is null)
                return result;

            await PersistMutationAsync(kiosk.Id, device, operation, normalizedKey!, requestFingerprint, normalizedReason!, oldScope, now, user.AccountId, ct);
            return result;
        }, cancellationToken);
    }

    private async Task<ApiResult<ClientDeviceResult>> ExecuteMutationAsync(
        Guid kioskId,
        string operation,
        string? idempotencyKey,
        string reason,
        CurrentUserContext user,
        IReadOnlyCollection<string> allowedRoles,
        string requestFingerprint,
        Func<Domain.Tenants.Entities.Kiosk, DateTimeOffset, Guid, CancellationToken, Task<ApiResult<ClientDeviceResult>>> action,
        CancellationToken cancellationToken)
    {
        return await store.ExecuteInTransactionAsync(async ct =>
        {
            var kiosk = await store.GetKioskAsync(kioskId, ct);
            if (kiosk is null || !CanAccess(allowedRoles, user, kiosk))
                return ApiResult<ClientDeviceResult>.Fail("Kiosk not found.", 404);
            if (!TryNormalizeMutation(reason, idempotencyKey, out var normalizedReason, out var normalizedKey, out var error))
                return ApiResult<ClientDeviceResult>.Fail(error!, 400);

            await store.AcquireKioskLockAsync(kiosk.Id, ct);
            var replay = await store.GetReplayAsync(kiosk.Id, operation, normalizedKey!, ct);
            if (replay is not null)
                return await ReplayResultAsync(replay, requestFingerprint, ct);

            var now = DateTimeOffset.UtcNow;
            var result = await action(kiosk, now, user.AccountId, ct);
            if (!result.Succeeded || result.Data is null)
                return result;

            // The device and its initial credential must be persisted before the
            // replay/audit record can reference the newly allocated device row.
            await store.SaveChangesAsync(ct);
            var device = await store.GetByIdAsync(result.Data.Id, tracking: true, ct);
            if (device is null)
                throw new InvalidOperationException("Provisioned client device was not found in the current transaction.");
            await PersistMutationAsync(kiosk.Id, device, operation, normalizedKey!, requestFingerprint, normalizedReason!, null, now, user.AccountId, ct);
            return result;
        }, cancellationToken);
    }

    private async Task PersistMutationAsync(
        Guid kioskId,
        ClientDevice device,
        string operation,
        string idempotencyKey,
        string fingerprint,
        string reason,
        object? previous,
        DateTimeOffset now,
        Guid actorAccountId,
        CancellationToken cancellationToken,
        Guid? replayClientDeviceId = null)
    {
        await store.AddReplayAsync(new ClientDeviceOperationReplay
        {
            KioskId = kioskId,
            ClientDeviceId = replayClientDeviceId ?? device.Id,
            Operation = operation,
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = fingerprint,
            ResultClientDeviceId = device.Id,
            CreatedAt = now,
            CreatedByAccountId = actorAccountId
        }, cancellationToken);
        await store.AddOperationLogAsync(new OperationLog
        {
            AccountId = actorAccountId,
            KioskId = kioskId,
            Action = $"ClientDevice.{operation}",
            Category = "ClientDevice",
            Severity = SeverityLevel.Info,
            Message = reason,
            PayloadJson = JsonSerializer.Serialize(new
            {
                ClientDeviceId = device.Id,
                Previous = previous,
                Current = new { device.OrganizationId, device.StoreId, device.KioskId, device.Status, device.Revision }
            }),
            OccurredAt = now,
            CreatedAt = now,
            CreatedByAccountId = actorAccountId
        }, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApiResult<ClientDeviceResult>> ReplayResultAsync(
        ClientDeviceOperationReplay replay,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(replay.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            return ApiResult<ClientDeviceResult>.Fail("Idempotency key was already used for a different request.", 409);

        var device = await store.GetByIdAsync(replay.ResultClientDeviceId, tracking: false, cancellationToken);
        return device is null
            ? ApiResult<ClientDeviceResult>.Fail("The prior idempotent operation result is unavailable.", 409)
            : ApiResult<ClientDeviceResult>.Success(ClientDeviceResultMapper.ToResult(device), "Client device operation already applied.");
    }

    private static bool CanAccess(IReadOnlyCollection<string> allowedRoles, CurrentUserContext user, Domain.Tenants.Entities.Kiosk kiosk) =>
        ScopeAccessRules.CanAccessScopedRow(allowedRoles, user, kiosk.OrganizationId, kiosk.StoreId, kiosk.Id);

    private static bool CanAccess(IReadOnlyCollection<string> allowedRoles, CurrentUserContext user, ClientDevice device) =>
        ScopeAccessRules.CanAccessScopedRow(allowedRoles, user, device.OrganizationId, device.StoreId, device.KioskId);

    private static bool TryNormalizeMutation(
        string? reason,
        string? idempotencyKey,
        out string? normalizedReason,
        out string? normalizedKey,
        out string? error)
    {
        normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
        {
            normalizedKey = null;
            error = "Operator reason is required.";
            return false;
        }
        if (!ScopedIdempotencyKey.TryNormalize(idempotencyKey, out normalizedKey))
        {
            error = $"Idempotency-Key is required and must be at most {ScopedIdempotencyKey.MaxClientKeyLength} characters.";
            return false;
        }

        error = null;
        return true;
    }

    private string CredentialFingerprint(string? credential)
    {
        if (!ClientDeviceCredentialHasher.TryDecodeCredential(credential, out _))
            return "INVALID";

        return Convert.ToHexString(credentialHasher.ComputeCurrent(credential!));
    }

    private static string Fingerprint(params object?[] values)
    {
        var payload = JsonSerializer.Serialize(values);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
