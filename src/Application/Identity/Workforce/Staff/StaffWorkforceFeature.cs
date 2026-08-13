using Application.Identity.Abstractions;
using Application.Identity.Invitations.Results;
using Application.Identity.Invitations.Services;
using Application.Identity.Tokens.Claims;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Application.Tenants;
using Application.Tenants.Abstractions;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Tenants.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace Application.Identity.Workforce.Staff;

public sealed class StaffWorkforceScopeRequest
{
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
}

public sealed class CreateStaffWorkforceRequest
{
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public bool LocalLoginEnabled { get; init; } = true;
    public bool GoogleLoginEnabled { get; init; }
    public string? GoogleEmail { get; init; }
    public bool SendInvitationEmail { get; init; } = true;
    public IReadOnlyList<StaffWorkforceScopeRequest> StaffScopes { get; init; } = [];
}

public sealed class UpdateStaffWorkforceRequest
{
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public long ExpectedRevision { get; init; }
}

public sealed class UpdateStaffWorkforceScopesRequest
{
    public IReadOnlyList<StaffWorkforceScopeRequest> StaffScopes { get; init; } = [];
    public long ExpectedRevision { get; init; }
}

public sealed class StaffLifecycleRequest
{
    [Required]
    [StringLength(128)]
    public string IdempotencyKey { get; init; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Reason { get; init; } = string.Empty;

    [Range(0, long.MaxValue)]
    public long ExpectedRevision { get; init; }
}

public sealed class StaffWorkforceResult
{
    public Guid AccountId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool LocalLoginEnabled { get; init; }
    public bool GoogleLoginEnabled { get; init; }
    public IReadOnlyList<StaffWorkforceScopeResult> StaffScopes { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public long Revision { get; init; }
    public StaffWorkforceInvitationResult? Invitation { get; init; }
}

public sealed class StaffWorkforceScopeResult
{
    public Guid? StoreId { get; init; }
    public string? StoreCode { get; init; }
    public Guid? KioskId { get; init; }
    public string? KioskCode { get; init; }
}

public sealed class StaffWorkforceInvitationResult
{
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? EmailSentAt { get; init; }
}

public sealed class ListStaffWorkforceQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class GetStaffWorkforceQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid AccountId { get; init; }
}

public sealed class CreateStaffWorkforceCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? ActorAccountId { get; init; }
    public string? IdempotencyKey { get; init; }
    public required CreateStaffWorkforceRequest Request { get; init; }
}

public sealed class UpdateStaffWorkforceCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid AccountId { get; init; }
    public Guid? ActorAccountId { get; init; }
    public required UpdateStaffWorkforceRequest Request { get; init; }
}

public sealed class UpdateStaffWorkforceScopesCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid AccountId { get; init; }
    public Guid? ActorAccountId { get; init; }
    public required UpdateStaffWorkforceScopesRequest Request { get; init; }
}

public sealed class ChangeStaffWorkforceLifecycleCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid AccountId { get; init; }
    public Guid? ActorAccountId { get; init; }
    public required StaffLifecycleRequest Request { get; init; }
    public bool Reactivate { get; init; }
}

public sealed class SendStaffWorkforceInvitationCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid AccountId { get; init; }
    public Guid? ActorAccountId { get; init; }
    public bool SendEmail { get; init; } = true;
}

public interface IStaffSessionRevoker
{
    Task<int> RevokeAllAsync(Guid accountId, string reason, CancellationToken cancellationToken = default);
}

public sealed class RefreshTokenStaffSessionRevoker(RefreshTokenService refreshTokens) : IStaffSessionRevoker
{
    public Task<int> RevokeAllAsync(Guid accountId, string reason, CancellationToken cancellationToken = default) =>
        refreshTokens.RevokeAllForAccountAsync(accountId, reason, ipAddress: null);
}

internal static class StaffWorkforceRules
{
    private static readonly string[] WorkforceRoles = ["OrgAdmin", "Manager"];

    public static bool CanManageStaff(CurrentUserContext user, Guid organizationId, Account account) =>
        account.Id != user.AccountId &&
        account.AccountRoles.Where(role => role.IsActive).Any() &&
        account.AccountRoles.Where(role => role.IsActive).All(role =>
            string.Equals(role.Role.Code, "Staff", StringComparison.OrdinalIgnoreCase) &&
            AccountBelongsToOrganization(role, organizationId) &&
            ScopeAccessRules.CanAccessScopedRow(WorkforceRoles, user, role.OrganizationId, role.StoreId, role.KioskId));

    public static bool AccountBelongsToOrganization(AccountRole role, Guid organizationId) =>
        role.OrganizationId == organizationId ||
        role.Store?.OrganizationId == organizationId ||
        role.Kiosk?.OrganizationId == organizationId;

    public static async Task<string?> ValidateScopesAsync(
        ITenantTreeStore tenantTree, CurrentUserContext user, Guid organizationId,
        IReadOnlyList<StaffWorkforceScopeRequest> scopes, CancellationToken cancellationToken)
    {
        if (scopes.Count == 0) return "At least one Staff store or kiosk scope is required.";
        if (scopes.GroupBy(scope => new { scope.StoreId, scope.KioskId }).Any(group => group.Count() > 1))
            return "A Staff scope can only be selected once.";

        var storeIds = scopes.Where(scope => scope.StoreId.HasValue).Select(scope => scope.StoreId!.Value).Distinct().ToArray();
        var kioskIds = scopes.Where(scope => scope.KioskId.HasValue).Select(scope => scope.KioskId!.Value).Distinct().ToArray();
        var stores = await tenantTree.ListStoresByIdsAsync(storeIds, includeInactive: false, cancellationToken);
        var kiosks = await tenantTree.ListKiosksByIdsAsync(kioskIds, includeInactive: false, cancellationToken);

        foreach (var scope in scopes)
        {
            if (!scope.StoreId.HasValue && !scope.KioskId.HasValue) return "Each Staff scope requires a store or kiosk.";
            if (scope.KioskId.HasValue && !scope.StoreId.HasValue) return "A kiosk Staff scope must include its store.";
            var store = scope.StoreId.HasValue ? stores.SingleOrDefault(item => item.Id == scope.StoreId.Value) : null;
            var kiosk = scope.KioskId.HasValue ? kiosks.SingleOrDefault(item => item.Id == scope.KioskId.Value) : null;
            if (store is not null && store.OrganizationId != organizationId) return "Staff store does not belong to the organization.";
            if (kiosk is not null && kiosk.OrganizationId != organizationId) return "Staff kiosk does not belong to the organization.";
            if (scope.StoreId.HasValue && store is null) return "Staff store is not active or does not exist.";
            if (scope.KioskId.HasValue && kiosk is null) return "Staff kiosk is not active or does not exist.";
            if (store is not null && kiosk is not null && kiosk.StoreId != store.Id) return "Staff kiosk does not belong to the selected store.";

            var targetStoreId = store?.Id ?? kiosk!.StoreId;
            var targetKioskId = kiosk?.Id;
            if (!ScopeAccessRules.CanAccessScopedRow(WorkforceRoles, user, organizationId, targetStoreId, targetKioskId))
                return "Current account is not allowed to assign this Staff scope.";
        }

        return null;
    }

    public static StaffWorkforceResult ToResult(Account account, AccountInvitationResult? invitation = null) => new()
    {
        AccountId = account.Id,
        UserName = account.UserName,
        Email = account.Email,
        FullName = account.FullName,
        PhoneNumber = account.PhoneNumber,
        Status = account.Status.ToString(),
        LocalLoginEnabled = account.LocalLoginEnabled,
        GoogleLoginEnabled = account.GoogleLoginEnabled,
        CreatedAt = account.CreatedAt,
        UpdatedAt = account.UpdatedAt,
        Revision = account.WorkforceRevision,
        StaffScopes = account.AccountRoles.Where(role => role.IsActive && role.Role.Code == "Staff").Select(role => new StaffWorkforceScopeResult
        {
            StoreId = role.StoreId,
            StoreCode = role.Store?.Code,
            KioskId = role.KioskId,
            KioskCode = role.Kiosk?.Code
        }).ToArray(),
        Invitation = invitation is null ? null : new StaffWorkforceInvitationResult { ExpiresAt = invitation.ExpiresAt, EmailSentAt = invitation.EmailSentAt }
    };

    public static string CreateFingerprint(CreateStaffWorkforceRequest request) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", request.UserName.Trim().ToUpperInvariant(), request.Email.Trim().ToUpperInvariant(), request.FullName?.Trim(), request.PhoneNumber?.Trim(), request.LocalLoginEnabled, request.GoogleLoginEnabled, request.GoogleEmail?.Trim().ToUpperInvariant(), string.Join(",", request.StaffScopes.OrderBy(x => x.StoreId).ThenBy(x => x.KioskId).Select(x => $"{x.StoreId:N}:{x.KioskId:N}"))))));
}

public sealed class ListStaffWorkforceQueryHandler(IIdentityAccountStore accounts)
{
    public async Task<PagedResult<StaffWorkforceResult>> HandleAsync(ListStaffWorkforceQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var scope = ScopeAccessRules.GetEffectiveScope(["OrgAdmin", "Manager"], query.UserContext);
        var count = await accounts.CountStaffAsync(query.Search, query.Status, query.OrganizationId, scope.OrganizationIds, scope.StoreIds, scope.KioskIds, cancellationToken);
        var values = await accounts.ListStaffAsync(query.Search, query.Status, query.OrganizationId, scope.OrganizationIds, scope.StoreIds, scope.KioskIds, page, pageSize, cancellationToken);
        return PagedResult<StaffWorkforceResult>.Success(values.Select(account => StaffWorkforceRules.ToResult(account)), count, page, pageSize);
    }
}

public sealed class GetStaffWorkforceQueryHandler(IIdentityAccountStore accounts)
{
    public async Task<ApiResult<StaffWorkforceResult>> HandleAsync(GetStaffWorkforceQuery query, CancellationToken cancellationToken = default)
    {
        var account = await accounts.GetByIdAsync(query.AccountId, asNoTracking: true, cancellationToken);
        if (account is null) return ApiResult<StaffWorkforceResult>.Fail("Staff account not found.", 404);
        return StaffWorkforceRules.CanManageStaff(query.UserContext, query.OrganizationId, account)
            ? ApiResult<StaffWorkforceResult>.Success(StaffWorkforceRules.ToResult(account))
            : ApiResult<StaffWorkforceResult>.Fail("Staff account is outside the current workforce scope.", 403);
    }
}

public sealed class CreateStaffWorkforceCommandHandler(
    IIdentityAccountStore accounts, ITenantTreeStore tenantTree, AccountInvitationService invitations)
{
    public async Task<ApiResult<StaffWorkforceResult>> HandleAsync(CreateStaffWorkforceCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length > 128)
            return ApiResult<StaffWorkforceResult>.Fail("Idempotency-Key is required and must not exceed 128 characters.");
        var key = command.IdempotencyKey.Trim();
        var fingerprint = StaffWorkforceRules.CreateFingerprint(request);
        var scopeError = await StaffWorkforceRules.ValidateScopesAsync(tenantTree, command.UserContext, command.OrganizationId, request.StaffScopes, cancellationToken);
        if (scopeError is not null) return ApiResult<StaffWorkforceResult>.Fail(scopeError, 403);
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email))
            return ApiResult<StaffWorkforceResult>.Fail("User name and email are required.");
        if (!request.LocalLoginEnabled && !request.GoogleLoginEnabled)
            return ApiResult<StaffWorkforceResult>.Fail("At least one authentication method must be enabled.");
        if (request.GoogleLoginEnabled && string.IsNullOrWhiteSpace(request.GoogleEmail))
            return ApiResult<StaffWorkforceResult>.Fail("Google email is required when Google login is enabled.");

        var email = Application.Identity.InternalAccounts.InternalAccountNormalizer.NormalizeEmail(request.Email);
        var userName = Application.Identity.InternalAccounts.InternalAccountNormalizer.NormalizeUserName(request.UserName);
        var googleEmail = request.GoogleLoginEnabled ? Application.Identity.InternalAccounts.InternalAccountNormalizer.NormalizeEmail(request.GoogleEmail!) : null;
        var staffRole = await accounts.GetRoleByCodeAsync("Staff", cancellationToken);
        if (staffRole is null) return ApiResult<StaffWorkforceResult>.Fail("Staff role is not configured.", 500);

        var persisted = await accounts.ExecuteStaffWorkforceTransactionAsync<StaffCreatePersistence>(async () =>
        {
            await accounts.AcquireStaffWorkforceCreateLockAsync(command.OrganizationId, key, cancellationToken);
            var replay = await accounts.GetStaffWorkforceCreateReplayAsync(command.OrganizationId, key, cancellationToken);
            if (replay is not null)
            {
                if (!string.Equals(replay.RequestFingerprint, fingerprint, StringComparison.Ordinal))
                    return StaffCreatePersistence.Failure("Idempotency key was already used with a different Staff request.", 409);
                var existing = await accounts.GetByIdAsync(replay.AccountId, asNoTracking: true, cancellationToken);
                return existing is null
                    ? StaffCreatePersistence.Failure("Staff create replay is incomplete.", 409)
                    : StaffCreatePersistence.Replay(existing);
            }

            if (await accounts.ExistsByEmailOrUserNameAsync(email, userName, cancellationToken))
                return StaffCreatePersistence.Failure("Account already exists.", 409);
            if (googleEmail is not null && await accounts.GoogleEmailExistsAsync(googleEmail, cancellationToken: cancellationToken))
                return StaffCreatePersistence.Failure("Google email already belongs to another account.", 409);

            var now = DateTimeOffset.UtcNow;
            var account = new Account
            {
                UserName = userName, Email = email, FullName = request.FullName?.Trim(), PhoneNumber = request.PhoneNumber?.Trim(),
                Status = AccountStatus.Invited, LocalLoginEnabled = request.LocalLoginEnabled, GoogleLoginEnabled = request.GoogleLoginEnabled,
                GoogleEmail = googleEmail, CreatedAt = now, CreatedByAccountId = command.ActorAccountId
            };
            foreach (var scope in request.StaffScopes)
                account.AccountRoles.Add(new AccountRole { Role = staffRole, RoleId = staffRole.Id, OrganizationId = command.OrganizationId,
                    StoreId = scope.StoreId, KioskId = scope.KioskId, AssignedAt = now, AssignedByAccountId = command.ActorAccountId });
            await accounts.AddAsync(account, cancellationToken);
            await accounts.AddStaffWorkforceCreateReplayAsync(new StaffWorkforceCreateReplay { OrganizationId = command.OrganizationId, IdempotencyKey = key, RequestFingerprint = fingerprint, AccountId = account.Id, CreatedAt = now, CreatedByAccountId = command.ActorAccountId }, cancellationToken);
            await accounts.SaveChangesAsync(cancellationToken);
            return StaffCreatePersistence.Created(account);
        }, cancellationToken);
        if (persisted.Error is not null) return ApiResult<StaffWorkforceResult>.Fail(persisted.Error, persisted.StatusCode);
        if (!persisted.CreatedNow) return ApiResult<StaffWorkforceResult>.Success(StaffWorkforceRules.ToResult(persisted.Account!), "Staff request was already completed.");

        var invitation = await invitations.CreateInvitationAsync(persisted.Account!, command.ActorAccountId, request.SendInvitationEmail, cancellationToken);
        if (!invitation.Succeeded || invitation.Data is null) return ApiResult<StaffWorkforceResult>.Fail(invitation.Message ?? "Staff invitation could not be created.", invitation.StatusCode);
        return ApiResult<StaffWorkforceResult>.Success(StaffWorkforceRules.ToResult(persisted.Account!, invitation.Data), invitation.Message ?? "Staff invited.", 201);
    }

    private sealed record StaffCreatePersistence(Account? Account, bool CreatedNow, string? Error, int StatusCode)
    {
        public static StaffCreatePersistence Created(Account account) => new(account, true, null, 201);
        public static StaffCreatePersistence Replay(Account account) => new(account, false, null, 200);
        public static StaffCreatePersistence Failure(string error, int statusCode) => new(null, false, error, statusCode);
    }
}

public sealed class UpdateStaffWorkforceCommandHandler(IIdentityAccountStore accounts)
{
    public async Task<ApiResult<StaffWorkforceResult>> HandleAsync(UpdateStaffWorkforceCommand command, CancellationToken cancellationToken = default)
    {
        return await accounts.ExecuteStaffWorkforceTransactionAsync(async () =>
        {
        await accounts.AcquireStaffWorkforceAccountLockAsync(command.AccountId, cancellationToken);
        var account = await accounts.GetByIdAsync(command.AccountId, asNoTracking: false, cancellationToken);
        if (account is null) return ApiResult<StaffWorkforceResult>.Fail("Staff account not found.", 404);
        if (!StaffWorkforceRules.CanManageStaff(command.UserContext, command.OrganizationId, account)) return ApiResult<StaffWorkforceResult>.Fail("Staff account is outside the current workforce scope.", 403);
        if (account.WorkforceRevision != command.Request.ExpectedRevision) return ApiResult<StaffWorkforceResult>.Fail("Staff account was changed by another user. Refresh and try again.", 409);
        account.FullName = command.Request.FullName?.Trim() ?? account.FullName;
        account.PhoneNumber = command.Request.PhoneNumber?.Trim() ?? account.PhoneNumber;
        account.UpdatedAt = DateTimeOffset.UtcNow;
        account.UpdatedByAccountId = command.ActorAccountId;
        account.WorkforceRevision++;
        await accounts.SaveChangesAsync(cancellationToken);
        return ApiResult<StaffWorkforceResult>.Success(StaffWorkforceRules.ToResult(account), "Staff updated.");
        }, cancellationToken);
    }
}

public sealed class UpdateStaffWorkforceScopesCommandHandler(IIdentityAccountStore accounts, ITenantTreeStore tenantTree)
{
    public async Task<ApiResult<StaffWorkforceResult>> HandleAsync(UpdateStaffWorkforceScopesCommand command, CancellationToken cancellationToken = default)
    {
        return await accounts.ExecuteStaffWorkforceTransactionAsync(async () =>
        {
        await accounts.AcquireStaffWorkforceAccountLockAsync(command.AccountId, cancellationToken);
        var account = await accounts.GetByIdAsync(command.AccountId, asNoTracking: false, cancellationToken);
        if (account is null) return ApiResult<StaffWorkforceResult>.Fail("Staff account not found.", 404);
        if (!StaffWorkforceRules.CanManageStaff(command.UserContext, command.OrganizationId, account)) return ApiResult<StaffWorkforceResult>.Fail("Staff account is outside the current workforce scope.", 403);
        if (account.WorkforceRevision != command.Request.ExpectedRevision) return ApiResult<StaffWorkforceResult>.Fail("Staff account was changed by another user. Refresh and try again.", 409);
        var scopeError = await StaffWorkforceRules.ValidateScopesAsync(tenantTree, command.UserContext, command.OrganizationId, command.Request.StaffScopes, cancellationToken);
        if (scopeError is not null) return ApiResult<StaffWorkforceResult>.Fail(scopeError, 403);
        var staffRole = await accounts.GetRoleByCodeAsync("Staff", cancellationToken);
        if (staffRole is null) return ApiResult<StaffWorkforceResult>.Fail("Staff role is not configured.", 500);
        foreach (var role in account.AccountRoles.Where(role => role.IsActive && role.Role.Code == "Staff")) role.IsActive = false;
        var now = DateTimeOffset.UtcNow;
        foreach (var scope in command.Request.StaffScopes)
        {
            var existingRole = account.AccountRoles.FirstOrDefault(role => role.RoleId == staffRole.Id &&
                role.OrganizationId == command.OrganizationId && role.StoreId == scope.StoreId && role.KioskId == scope.KioskId);
            if (existingRole is not null)
            {
                existingRole.IsActive = true;
                existingRole.AssignedAt = now;
                existingRole.AssignedByAccountId = command.ActorAccountId;
            }
            else
            {
                account.AccountRoles.Add(new AccountRole { Role = staffRole, RoleId = staffRole.Id, OrganizationId = command.OrganizationId,
                    StoreId = scope.StoreId, KioskId = scope.KioskId, AssignedAt = now, AssignedByAccountId = command.ActorAccountId });
            }
        }
        account.UpdatedAt = now; account.UpdatedByAccountId = command.ActorAccountId;
        account.WorkforceRevision++;
        await accounts.SaveChangesAsync(cancellationToken);
        return ApiResult<StaffWorkforceResult>.Success(StaffWorkforceRules.ToResult(account), "Staff scopes updated.");
        }, cancellationToken);
    }
}

public sealed class ChangeStaffWorkforceLifecycleCommandHandler(IIdentityAccountStore accounts, IStaffSessionRevoker sessionRevoker)
{
    public async Task<ApiResult<StaffWorkforceResult>> HandleAsync(ChangeStaffWorkforceLifecycleCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Reason) || string.IsNullOrWhiteSpace(command.Request.IdempotencyKey) || command.Request.IdempotencyKey.Trim().Length > 128)
            return ApiResult<StaffWorkforceResult>.Fail("A lifecycle reason and an Idempotency-Key of at most 128 characters are required.");
        var key = command.Request.IdempotencyKey.Trim();
        var persisted = await accounts.ExecuteStaffWorkforceTransactionAsync<StaffLifecyclePersistence>(async () =>
        {
        await accounts.AcquireStaffWorkforceAccountLockAsync(command.AccountId, cancellationToken);
        var existing = await accounts.GetStaffWorkforceLifecycleTransitionByIdempotencyKeyAsync(command.OrganizationId, key, cancellationToken);
        if (existing is not null)
        {
            if (existing.AccountId != command.AccountId || existing.ToStatus != (command.Reactivate ? AccountStatus.Active : AccountStatus.Disabled) || !string.Equals(existing.Reason, command.Request.Reason.Trim(), StringComparison.Ordinal))
                return StaffLifecyclePersistence.Failure("Idempotency key was already used for a different Staff lifecycle transition.", 409);
            var replay = await accounts.GetByIdAsync(command.AccountId, asNoTracking: true, cancellationToken);
            return replay is null ? StaffLifecyclePersistence.Failure("Staff account not found.", 404) : StaffLifecyclePersistence.Replay(replay, !command.Reactivate);
        }
        var account = await accounts.GetByIdAsync(command.AccountId, asNoTracking: false, cancellationToken);
        if (account is null) return StaffLifecyclePersistence.Failure("Staff account not found.", 404);
        if (!StaffWorkforceRules.CanManageStaff(command.UserContext, command.OrganizationId, account)) return StaffLifecyclePersistence.Failure("Staff account is outside the current workforce scope.", 403);
        if (account.WorkforceRevision != command.Request.ExpectedRevision) return StaffLifecyclePersistence.Failure("Staff account was changed by another user. Refresh and try again.", 409);

        var previousStatus = account.Status;
        if (command.Reactivate)
        {
            if (account.Status != AccountStatus.Disabled) return StaffLifecyclePersistence.Failure("Only a disabled Staff account can be reactivated.", 409);
            account.Status = AccountStatus.Active;
        }
        else
        {
            if (account.Status is not (AccountStatus.Active or AccountStatus.Invited)) return StaffLifecyclePersistence.Failure("Only an active or invited Staff account can be deactivated.", 409);
            account.Status = AccountStatus.Disabled;
        }

        account.UpdatedAt = DateTimeOffset.UtcNow;
        account.UpdatedByAccountId = command.ActorAccountId;
        account.WorkforceRevision++;
        var authorizingScope = account.AccountRoles.Where(role => role.IsActive).SelectMany(role => ScopeAccessRules.GetAuthorizingScopeSnapshots(["OrgAdmin", "Manager"], command.UserContext, role.OrganizationId, role.StoreId, role.KioskId)).FirstOrDefault();
        if (authorizingScope is null) return StaffLifecyclePersistence.Failure("Staff account is outside the current workforce scope.", 403);
        await accounts.AddStaffWorkforceLifecycleTransitionAsync(new StaffWorkforceLifecycleTransition { OrganizationId = command.OrganizationId, AccountId = account.Id, FromStatus = previousStatus, ToStatus = account.Status, Reason = command.Request.Reason.Trim(), ActorRoleCode = authorizingScope.RoleCode, ActorOrganizationId = authorizingScope.OrganizationId, ActorStoreId = authorizingScope.StoreId, RequestIdempotencyKey = key, WorkforceRevision = account.WorkforceRevision, CreatedAt = DateTimeOffset.UtcNow, CreatedByAccountId = command.ActorAccountId }, cancellationToken);
        await accounts.SaveChangesAsync(cancellationToken);
        return StaffLifecyclePersistence.Applied(account, !command.Reactivate);
        }, cancellationToken);
        if (persisted.Error is not null) return ApiResult<StaffWorkforceResult>.Fail(persisted.Error, persisted.StatusCode);
        if (!persisted.SessionRevocationRequired) return ApiResult<StaffWorkforceResult>.Success(StaffWorkforceRules.ToResult(persisted.Account!), command.Reactivate ? "Staff reactivated." : "Staff deactivated.");
        try
        {
            await sessionRevoker.RevokeAllAsync(persisted.Account!.Id, "Staff disabled by workforce manager", cancellationToken);
            return ApiResult<StaffWorkforceResult>.Success(StaffWorkforceRules.ToResult(persisted.Account), persisted.Replayed ? "Staff lifecycle transition was already applied; sessions were rechecked." : "Staff deactivated.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResult<StaffWorkforceResult>.Success(StaffWorkforceRules.ToResult(persisted.Account!), "Staff was disabled. Session revocation is pending reconciliation.", 202)
                .AddDetail("sessionRevocation", "pending");
        }
    }

    private sealed record StaffLifecyclePersistence(Account? Account, bool SessionRevocationRequired, bool Replayed, string? Error, int StatusCode)
    {
        public static StaffLifecyclePersistence Applied(Account account, bool revoke) => new(account, revoke, false, null, 200);
        public static StaffLifecyclePersistence Replay(Account account, bool revoke) => new(account, revoke, true, null, 200);
        public static StaffLifecyclePersistence Failure(string error, int statusCode) => new(null, false, false, error, statusCode);
    }
}

public sealed class SendStaffWorkforceInvitationCommandHandler(IIdentityAccountStore accounts, AccountInvitationService invitations)
{
    public async Task<ApiResult<StaffWorkforceResult>> HandleAsync(SendStaffWorkforceInvitationCommand command, CancellationToken cancellationToken = default)
    {
        var account = await accounts.GetByIdAsync(command.AccountId, asNoTracking: false, cancellationToken);
        if (account is null) return ApiResult<StaffWorkforceResult>.Fail("Staff account not found.", 404);
        if (!StaffWorkforceRules.CanManageStaff(command.UserContext, command.OrganizationId, account)) return ApiResult<StaffWorkforceResult>.Fail("Staff account is outside the current workforce scope.", 403);
        if (account.Status != AccountStatus.Invited) return ApiResult<StaffWorkforceResult>.Fail("Invitation can only be created for invited Staff accounts.", 409);
        var invitation = await invitations.CreateInvitationAsync(account, command.ActorAccountId, command.SendEmail, cancellationToken);
        return invitation.Succeeded && invitation.Data is not null
            ? ApiResult<StaffWorkforceResult>.Success(StaffWorkforceRules.ToResult(account, invitation.Data), invitation.Message ?? "Staff invitation created.", invitation.StatusCode)
            : ApiResult<StaffWorkforceResult>.Fail(invitation.Message ?? "Staff invitation could not be created.", invitation.StatusCode);
    }
}
