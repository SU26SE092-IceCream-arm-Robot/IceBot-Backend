using System.Security.Cryptography;
using System.Text;
using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts;
using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Domain.Identity.Entities;

namespace Application.Identity.PlatformTechnicians;

public sealed class ReplacePlatformTechnicianScopesCommandHandler(
    IIdentityAccountStore accounts,
    ITenantTreeStore tenantTree)
{
    public async Task<ApiResult<TechnicianResult>> HandleAsync(
        Guid accountId,
        Guid? actorId,
        string key,
        ReplaceTechnicianScopesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128 ||
            string.IsNullOrWhiteSpace(request.Reason))
        {
            return ApiResult<TechnicianResult>.Fail(
                "Idempotency-Key and reason are required.",
                400);
        }

        var normalizedKey = key.Trim();
        var fingerprint = CreateFingerprint(request);
        return await accounts.ExecuteInTransactionAsync(async () =>
        {
            await accounts.AcquireAccountLockAsync(accountId, cancellationToken);
            var replay = await accounts.GetTechnicianScopeReplayAsync(
                accountId,
                normalizedKey,
                cancellationToken);
            if (replay is not null)
            {
                if (replay.RequestFingerprint != fingerprint)
                {
                    return ApiResult<TechnicianResult>.Fail(
                        "Idempotency key was already used with a different request.",
                        409);
                }

                var previous = await accounts.GetByIdAsync(accountId, true, cancellationToken);
                return ApiResult<TechnicianResult>.Success(
                    PlatformTechnicianResultMapper.ToResult(previous!),
                    "Technician scopes were already updated.");
            }

            var account = await accounts.GetByIdAsync(accountId, false, cancellationToken);
            if (account?.PlatformTechnicianProfile is null ||
                PlatformTechnicianBoundary.HasMixedActiveRoles(account))
            {
                return ApiResult<TechnicianResult>.Fail("Technician account not found.", 404);
            }

            if (account.AuthorizationVersion != request.ExpectedAuthorizationVersion)
            {
                return ApiResult<TechnicianResult>.Fail(
                    "Technician access changed by another user. Refresh and try again.",
                    409);
            }

            var validationError = await ValidateScopesAsync(request.Scopes, cancellationToken);
            if (validationError is not null)
            {
                return ApiResult<TechnicianResult>.Fail(validationError, 400);
            }

            var now = DateTimeOffset.UtcNow;
            var nextVersion = account.AuthorizationVersion + 1;
            foreach (var grant in account.TechnicianSupportGrants.Where(grant => grant.IsActive))
            {
                grant.Revoke(now, actorId);
                await AddHistoryAsync(
                    account.Id,
                    grant.OrganizationId,
                    grant.StoreId,
                    grant.KioskId,
                    "Revoked",
                    request.Reason,
                    nextVersion,
                    actorId,
                    now,
                    cancellationToken);
            }

            foreach (var scope in request.Scopes)
            {
                account.TechnicianSupportGrants.Add(TechnicianSupportGrant.Create(
                    account.Id,
                    scope.OrganizationId,
                    scope.StoreId,
                    scope.KioskId,
                    now,
                    actorId));
                await AddHistoryAsync(
                    account.Id,
                    scope.OrganizationId,
                    scope.StoreId,
                    scope.KioskId,
                    "Granted",
                    request.Reason,
                    nextVersion,
                    actorId,
                    now,
                    cancellationToken);
            }

            account.AuthorizationVersion = nextVersion;
            account.UpdatedAt = now;
            account.UpdatedByAccountId = actorId;
            await accounts.AddTechnicianScopeReplayAsync(new TechnicianSupportScopeReplay
            {
                AccountId = accountId,
                IdempotencyKey = normalizedKey,
                RequestFingerprint = fingerprint,
                AuthorizationVersion = nextVersion,
                CreatedAt = now,
                CreatedByAccountId = actorId
            }, cancellationToken);
            await accounts.SaveChangesAsync(cancellationToken);

            return ApiResult<TechnicianResult>.Success(
                PlatformTechnicianResultMapper.ToResult(account),
                "Technician support scopes updated.");
        }, cancellationToken);
    }

    private async Task AddHistoryAsync(
        Guid accountId,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string action,
        string reason,
        long authorizationVersion,
        Guid? actorId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await accounts.AddTechnicianGrantHistoryAsync(new TechnicianSupportGrantHistory
        {
            AccountId = accountId,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            Action = action,
            Reason = reason.Trim(),
            AuthorizationVersion = authorizationVersion,
            ActorAccountId = actorId,
            CreatedAt = now,
            CreatedByAccountId = actorId
        }, cancellationToken);

    private async Task<string?> ValidateScopesAsync(
        IReadOnlyList<TechnicianScopeRequest> scopes,
        CancellationToken cancellationToken)
    {
        if (scopes.GroupBy(scope => new { scope.OrganizationId, scope.StoreId, scope.KioskId })
            .Any(group => group.Count() > 1))
        {
            return "Duplicate support scope.";
        }

        foreach (var scope in scopes)
        {
            if (scope.OrganizationId == Guid.Empty ||
                scope.StoreId.HasValue == scope.KioskId.HasValue)
            {
                return "Each support scope requires exactly one store or kiosk.";
            }

            if (scope.StoreId is { } storeId)
            {
                var store = (await tenantTree.ListStoresByIdsAsync([storeId], false, cancellationToken))
                    .SingleOrDefault();
                if (store is null || store.OrganizationId != scope.OrganizationId)
                {
                    return "Support scope does not resolve to an active store in its organization.";
                }
            }
            else if (scope.KioskId is { } kioskId)
            {
                var kiosk = (await tenantTree.ListKiosksByIdsAsync([kioskId], false, cancellationToken))
                    .SingleOrDefault();
                if (kiosk is null || kiosk.OrganizationId != scope.OrganizationId)
                {
                    return "Support scope does not resolve to an active kiosk in its organization.";
                }
            }
        }

        return null;
    }

    private static string CreateFingerprint(ReplaceTechnicianScopesRequest request)
    {
        var scopes = string.Join(",", request.Scopes
            .OrderBy(scope => scope.OrganizationId)
            .ThenBy(scope => scope.StoreId)
            .ThenBy(scope => scope.KioskId)
            .Select(scope => $"{scope.OrganizationId:N}:{scope.StoreId:N}:{scope.KioskId:N}"));
        var source = $"{request.ExpectedAuthorizationVersion}|{request.Reason.Trim()}|{scopes}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }
}
