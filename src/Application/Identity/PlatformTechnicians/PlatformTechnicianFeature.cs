using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts;
using Application.Identity.Provisioning;
using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Application.Identity.PlatformTechnicians;

public sealed record TechnicianScopeRequest(Guid OrganizationId, Guid? StoreId, Guid? KioskId);
public sealed record CreatePlatformTechnicianRequest(string UserName, string Email, string? FullName, string? PhoneNumber);
public sealed record UpdatePlatformTechnicianRequest(string? FullName, string? PhoneNumber, long ExpectedAuthorizationVersion);
public sealed record TechnicianLifecycleRequest(string Reason, long ExpectedAuthorizationVersion);
public sealed record ReplaceTechnicianScopesRequest(long ExpectedAuthorizationVersion, string Reason, IReadOnlyList<TechnicianScopeRequest> Scopes);
public sealed record TechnicianResult(Guid AccountId, string UserName, string Email, string Status, long AuthorizationVersion, IReadOnlyList<TechnicianScopeRequest> SupportScopes);

public sealed class ListPlatformTechniciansQueryHandler(IIdentityAccountStore accounts)
{
    public async Task<PagedResult<TechnicianResult>> HandleAsync(string? search, int page, int size, CancellationToken ct = default)
    {
        page = Math.Max(1, page); size = Math.Clamp(size, 1, 100);
        var items = await accounts.ListTechniciansAsync(search, page, size, ct);
        return PagedResult<TechnicianResult>.Success(items.Select(ToResult), await accounts.CountTechniciansAsync(search, ct), page, size);
    }

    public async Task<ApiResult<TechnicianResult>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var account = await accounts.GetByIdAsync(id, true, ct);
        return account?.PlatformTechnicianProfile is null || PlatformTechnicianBoundary.HasMixedActiveRoles(account)
            ? ApiResult<TechnicianResult>.Fail("Technician account not found.", 404)
            : ApiResult<TechnicianResult>.Success(ToResult(account));
    }

    internal static TechnicianResult ToResult(Account account) => new(account.Id, account.UserName, account.Email, account.Status.ToString(), account.AuthorizationVersion,
        account.TechnicianSupportGrants.Where(x => x.IsActive)
            .Select(x => new TechnicianScopeRequest(x.OrganizationId, x.StoreId, x.KioskId)).ToArray());
}

public sealed class ReplacePlatformTechnicianScopesCommandHandler(IIdentityAccountStore accounts, ITenantTreeStore tenantTree)
{
    public async Task<ApiResult<TechnicianResult>> HandleAsync(Guid accountId, Guid? actorId, string key, ReplaceTechnicianScopesRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128 || string.IsNullOrWhiteSpace(request.Reason)) return ApiResult<TechnicianResult>.Fail("Idempotency-Key and reason are required.", 400);
        var normalizedKey = key.Trim();
        var fingerprint = Fingerprint(request);
        return await accounts.ExecuteInTransactionAsync(async () =>
        {
            await accounts.AcquireAccountLockAsync(accountId, ct);
            var replay = await accounts.GetTechnicianScopeReplayAsync(accountId, normalizedKey, ct);
            if (replay is not null)
            {
                if (replay.RequestFingerprint != fingerprint) return ApiResult<TechnicianResult>.Fail("Idempotency key was already used with a different request.", 409);
                var prior = await accounts.GetByIdAsync(accountId, true, ct);
                return ApiResult<TechnicianResult>.Success(ListPlatformTechniciansQueryHandler.ToResult(prior!), "Technician scopes were already updated.");
            }
            var account = await accounts.GetByIdAsync(accountId, false, ct);
            if (account?.PlatformTechnicianProfile is null || PlatformTechnicianBoundary.HasMixedActiveRoles(account)) return ApiResult<TechnicianResult>.Fail("Technician account not found.", 404);
            if (account.AuthorizationVersion != request.ExpectedAuthorizationVersion) return ApiResult<TechnicianResult>.Fail("Technician access changed by another user. Refresh and try again.", 409);
            var validation = await ValidateAsync(request.Scopes, ct);
            if (validation is not null) return ApiResult<TechnicianResult>.Fail(validation, 400);
            var now = DateTimeOffset.UtcNow; var nextVersion = account.AuthorizationVersion + 1;
            foreach (var grant in account.TechnicianSupportGrants.Where(x => x.IsActive))
            {
                grant.Revoke(now, actorId);
                await History(account.Id, grant.OrganizationId, grant.StoreId, grant.KioskId, "Revoked", request.Reason, nextVersion, actorId, now, ct);
            }
            foreach (var scope in request.Scopes)
            {
                account.TechnicianSupportGrants.Add(TechnicianSupportGrant.Create(
                    account.Id, scope.OrganizationId, scope.StoreId, scope.KioskId, now, actorId));
                await History(account.Id, scope.OrganizationId, scope.StoreId, scope.KioskId, "Granted", request.Reason, nextVersion, actorId, now, ct);
            }
            account.AuthorizationVersion = nextVersion; account.UpdatedAt = now; account.UpdatedByAccountId = actorId;
            await accounts.AddTechnicianScopeReplayAsync(new TechnicianSupportScopeReplay { AccountId = accountId, IdempotencyKey = normalizedKey, RequestFingerprint = fingerprint, AuthorizationVersion = nextVersion, CreatedAt = now, CreatedByAccountId = actorId }, ct);
            await accounts.SaveChangesAsync(ct);
            return ApiResult<TechnicianResult>.Success(ListPlatformTechniciansQueryHandler.ToResult(account), "Technician support scopes updated.");
        }, ct);
    }

    private async Task History(Guid accountId, Guid? org, Guid? store, Guid? kiosk, string action, string reason, long version, Guid? actor, DateTimeOffset now, CancellationToken ct) =>
        await accounts.AddTechnicianGrantHistoryAsync(new TechnicianSupportGrantHistory { AccountId = accountId, OrganizationId = org, StoreId = store, KioskId = kiosk, Action = action, Reason = reason.Trim(), AuthorizationVersion = version, ActorAccountId = actor, CreatedAt = now, CreatedByAccountId = actor }, ct);

    private async Task<string?> ValidateAsync(IReadOnlyList<TechnicianScopeRequest> scopes, CancellationToken ct)
    {
        if (scopes.GroupBy(x => new { x.OrganizationId, x.StoreId, x.KioskId }).Any(x => x.Count() > 1)) return "Duplicate support scope.";
        foreach (var scope in scopes)
        {
            if (scope.OrganizationId == Guid.Empty || scope.StoreId.HasValue == scope.KioskId.HasValue) return "Each support scope requires exactly one store or kiosk.";
            if (scope.StoreId is { } storeId)
            {
                var store = (await tenantTree.ListStoresByIdsAsync([storeId], false, ct)).SingleOrDefault();
                if (store is null || store.OrganizationId != scope.OrganizationId) return "Support scope does not resolve to an active store in its organization.";
            }
            else if (scope.KioskId is { } kioskId)
            {
                var kiosk = (await tenantTree.ListKiosksByIdsAsync([kioskId], false, ct)).SingleOrDefault();
                if (kiosk is null || kiosk.OrganizationId != scope.OrganizationId) return "Support scope does not resolve to an active kiosk in its organization.";
            }
        }
        return null;
    }

    private static string Fingerprint(ReplaceTechnicianScopesRequest request) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{request.ExpectedAuthorizationVersion}|{request.Reason.Trim()}|{string.Join(',', request.Scopes.OrderBy(x => x.OrganizationId).ThenBy(x => x.StoreId).ThenBy(x => x.KioskId).Select(x => $"{x.OrganizationId:N}:{x.StoreId:N}:{x.KioskId:N}"))}")));
}

public sealed class PlatformTechnicianAccountCommandHandler(IIdentityAccountStore accounts, TenantAccountCredentialService credentials)
{
    public async Task<ApiResult<TechnicianResult>> CreateAsync(CreatePlatformTechnicianRequest request, Guid? actorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email)) return ApiResult<TechnicianResult>.Fail("User name and email are required.", 400);
        var email = InternalAccountNormalizer.NormalizeEmail(request.Email); var userName = InternalAccountNormalizer.NormalizeUserName(request.UserName);
        if (await accounts.ExistsByEmailOrUserNameAsync(email, userName, ct)) return ApiResult<TechnicianResult>.Fail("Account already exists.", 409);
        var now = DateTimeOffset.UtcNow;
        var account = new Account { UserName = userName, Email = email, FullName = request.FullName?.Trim(), PhoneNumber = request.PhoneNumber?.Trim(), Status = AccountStatus.Active, LocalLoginEnabled = true, CreatedAt = now, CreatedByAccountId = actorId, PlatformTechnicianProfile = new PlatformTechnicianProfile { CreatedAt = now, CreatedByAccountId = actorId } };
        var issued = credentials.Prepare(account, now); await accounts.AddAsync(account, ct); await accounts.SaveChangesAsync(ct); await credentials.TrySendAsync(account, issued, ct);
        return ApiResult<TechnicianResult>.Success(ListPlatformTechniciansQueryHandler.ToResult(account), "Technician account created.", 201);
    }

    public Task<ApiResult<TechnicianResult>> UpdateAsync(Guid id, UpdatePlatformTechnicianRequest request, Guid? actorId, CancellationToken ct) => MutateAsync(id, request.ExpectedAuthorizationVersion, actorId, ct, a => { a.FullName = request.FullName?.Trim() ?? a.FullName; a.PhoneNumber = request.PhoneNumber?.Trim() ?? a.PhoneNumber; return null; }, "Technician updated.");
    public Task<ApiResult<TechnicianResult>> LifecycleAsync(Guid id, TechnicianLifecycleRequest request, bool activate, Guid? actorId, CancellationToken ct) => MutateAsync(id, request.ExpectedAuthorizationVersion, actorId, ct, a => { if (string.IsNullOrWhiteSpace(request.Reason)) return "Reason is required."; if (activate ? a.Status != AccountStatus.Disabled : a.Status != AccountStatus.Active) return "Technician lifecycle transition is invalid."; a.Status = activate ? AccountStatus.Active : AccountStatus.Disabled; return null; }, activate ? "Technician reactivated." : "Technician deactivated.");
    private Task<ApiResult<TechnicianResult>> MutateAsync(Guid id, long version, Guid? actor, CancellationToken ct, Func<Account, string?> mutation, string message) => accounts.ExecuteInTransactionAsync(async () => { await accounts.AcquireAccountLockAsync(id, ct); var account = await accounts.GetByIdAsync(id, false, ct); if (account?.PlatformTechnicianProfile is null || PlatformTechnicianBoundary.HasMixedActiveRoles(account)) return ApiResult<TechnicianResult>.Fail("Technician account not found.", 404); if (account.AuthorizationVersion != version) return ApiResult<TechnicianResult>.Fail("Technician access changed by another user.", 409); var error = mutation(account); if (error is not null) return ApiResult<TechnicianResult>.Fail(error, 409); account.AuthorizationVersion++; account.UpdatedAt = DateTimeOffset.UtcNow; account.UpdatedByAccountId = actor; await accounts.SaveChangesAsync(ct); return ApiResult<TechnicianResult>.Success(ListPlatformTechniciansQueryHandler.ToResult(account), message); }, ct);
}
