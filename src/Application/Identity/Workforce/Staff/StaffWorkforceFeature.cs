using Application.Identity.Invitations.Services;
using Application.Identity.Invitations.Results;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Application.Tenants;
using Application.Tenants.Abstractions;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Tenants.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Identity.Workforce.Staff;

public sealed class RefreshTokenStaffSessionRevoker(RefreshTokenService refreshTokens) : IStaffSessionRevoker
{
    public Task<int> RevokeAllAsync(Guid accountId, string reason, CancellationToken cancellationToken = default) =>
        refreshTokens.RevokeAllForAccountAsync(accountId, reason, ipAddress: null);
}

public sealed class ListStaffWorkforceQueryHandler(IStaffWorkforceStore accounts)
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

public sealed class GetStaffWorkforceQueryHandler(IStaffWorkforceStore accounts)
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
    IStaffWorkforceStore accounts, ITenantTreeStore tenantTree, AccountInvitationService invitations)
{
    public async Task<ApiResult<StaffWorkforceResult>> HandleAsync(CreateStaffWorkforceCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length > 128)
            return ApiResult<StaffWorkforceResult>.Fail("Idempotency-Key is required and must not exceed 128 characters.");
        var key = command.IdempotencyKey.Trim();
        var fingerprint = StaffWorkforceRules.CreateFingerprint(request);
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email))
            return ApiResult<StaffWorkforceResult>.Fail("User name and email are required.");
        if (!request.LocalLoginEnabled && !request.GoogleLoginEnabled)
            return ApiResult<StaffWorkforceResult>.Fail("At least one authentication method must be enabled.");
        if (request.GoogleLoginEnabled && string.IsNullOrWhiteSpace(request.GoogleEmail))
            return ApiResult<StaffWorkforceResult>.Fail("Google email is required when Google login is enabled.");

        var email = Application.Identity.InternalAccounts.InternalAccountNormalizer.NormalizeEmail(request.Email);
        var userName = Application.Identity.InternalAccounts.InternalAccountNormalizer.NormalizeUserName(request.UserName);
        var googleEmail = request.GoogleLoginEnabled ? Application.Identity.InternalAccounts.InternalAccountNormalizer.NormalizeEmail(request.GoogleEmail!) : null;
        var persisted = await accounts.ExecuteTransactionAsync<StaffCreatePersistence>(async () =>
        {
            await accounts.AcquireCreateLockAsync(command.OrganizationId, key, cancellationToken);
            var replay = await accounts.GetCreateReplayAsync(command.OrganizationId, key, cancellationToken);
            if (replay is not null)
            {
                if (!string.Equals(replay.RequestFingerprint, fingerprint, StringComparison.Ordinal))
                    return StaffCreatePersistence.Failure("Idempotency key was already used with a different Staff request.", 409);
                var existing = await accounts.GetByIdAsync(replay.AccountId, asNoTracking: true, cancellationToken);
                return existing is null
                    ? StaffCreatePersistence.Failure("Staff create replay is incomplete.", 409)
                    : StaffCreatePersistence.Replay(existing);
            }

            await accounts.AcquireCreateIdentityLocksAsync(
                [
                    $"email:{email}",
                    $"username:{userName}",
                    googleEmail is null ? string.Empty : $"google-email:{googleEmail}"
                ],
                cancellationToken);
            var scopeError = await StaffWorkforceRules.ValidateScopesAsync(tenantTree, command.UserContext, command.OrganizationId, request.StaffScopes, cancellationToken);
            if (scopeError is not null) return StaffCreatePersistence.Failure(scopeError, 403);
            var staffRole = await accounts.GetRoleByCodeAsync("Staff", cancellationToken);
            if (staffRole is null) return StaffCreatePersistence.Failure("Staff role is not configured.", 500);

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
            await accounts.AddCreateReplayAsync(new StaffWorkforceCreateReplay { OrganizationId = command.OrganizationId, IdempotencyKey = key, RequestFingerprint = fingerprint, AccountId = account.Id, CreatedAt = now, CreatedByAccountId = command.ActorAccountId }, cancellationToken);
            await accounts.SaveChangesAsync(cancellationToken);
            return StaffCreatePersistence.Created(account);
        }, cancellationToken);
        if (persisted.Error is not null) return ApiResult<StaffWorkforceResult>.Fail(persisted.Error, persisted.StatusCode);
        if (persisted.Account is null) return ApiResult<StaffWorkforceResult>.Fail("Staff persistence did not return an account.", 500);
        var account = persisted.Account;
        if (!persisted.CreatedNow)
        {
            var ensuredInvitation = await invitations.EnsureActiveInvitationAsync(
                account,
                command.ActorAccountId,
                request.SendInvitationEmail,
                cancellationToken);
            if (!ensuredInvitation.Succeeded || ensuredInvitation.Data is null)
                return ApiResult<StaffWorkforceResult>.Fail(ensuredInvitation.Message ?? "Staff invitation could not be recovered.", ensuredInvitation.StatusCode);
            return ApiResult<StaffWorkforceResult>.Success(
                StaffWorkforceRules.ToResult(account),
                ensuredInvitation.Data.Created ? "Staff request replay recovered a missing invitation." : "Staff request was already completed.");
        }

        var invitation = await invitations.CreateInvitationAsync(account, command.ActorAccountId, request.SendInvitationEmail, cancellationToken);
        if (!invitation.Succeeded || invitation.Data is null) return ApiResult<StaffWorkforceResult>.Fail(invitation.Message ?? "Staff invitation could not be created.", invitation.StatusCode);
        return ApiResult<StaffWorkforceResult>.Success(StaffWorkforceRules.ToResult(account, invitation.Data), invitation.Message ?? "Staff invited.", 201);
    }

    private sealed record StaffCreatePersistence(Account? Account, bool CreatedNow, string? Error, int StatusCode)
    {
        public static StaffCreatePersistence Created(Account account) => new(account, true, null, 201);
        public static StaffCreatePersistence Replay(Account account) => new(account, false, null, 200);
        public static StaffCreatePersistence Failure(string error, int statusCode) => new(null, false, error, statusCode);
    }
}

public sealed class UpdateStaffWorkforceCommandHandler(IStaffWorkforceStore accounts)
{
    public async Task<ApiResult<StaffWorkforceResult>> HandleAsync(UpdateStaffWorkforceCommand command, CancellationToken cancellationToken = default)
    {
        return await accounts.ExecuteTransactionAsync(async () =>
        {
            await accounts.AcquireAccountLockAsync(command.AccountId, cancellationToken);
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

public sealed class UpdateStaffWorkforceScopesCommandHandler(IStaffWorkforceStore accounts, ITenantTreeStore tenantTree)
{
    public async Task<ApiResult<StaffWorkforceResult>> HandleAsync(UpdateStaffWorkforceScopesCommand command, CancellationToken cancellationToken = default)
    {
        return await accounts.ExecuteTransactionAsync(async () =>
        {
            await accounts.AcquireAccountLockAsync(command.AccountId, cancellationToken);
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

public sealed class ChangeStaffWorkforceLifecycleCommandHandler(IStaffWorkforceStore accounts, IStaffSessionRevoker sessionRevoker, ILogger<ChangeStaffWorkforceLifecycleCommandHandler> logger)
{
    public async Task<ApiResult<StaffWorkforceResult>> HandleAsync(ChangeStaffWorkforceLifecycleCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Reason) || string.IsNullOrWhiteSpace(command.Request.IdempotencyKey) || command.Request.IdempotencyKey.Trim().Length > 128)
            return ApiResult<StaffWorkforceResult>.Fail("A lifecycle reason and an Idempotency-Key of at most 128 characters are required.");
        var key = command.Request.IdempotencyKey.Trim();
        var persisted = await accounts.ExecuteTransactionAsync<StaffLifecyclePersistence>(async () =>
        {
            await accounts.AcquireAccountLockAsync(command.AccountId, cancellationToken);
            var existing = await accounts.GetLifecycleTransitionByIdempotencyKeyAsync(command.OrganizationId, key, cancellationToken);
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
            await accounts.AddLifecycleTransitionAsync(new StaffWorkforceLifecycleTransition { OrganizationId = command.OrganizationId, AccountId = account.Id, FromStatus = previousStatus, ToStatus = account.Status, Reason = command.Request.Reason.Trim(), ActorRoleCode = authorizingScope.RoleCode, ActorOrganizationId = authorizingScope.OrganizationId, ActorStoreId = authorizingScope.StoreId, RequestIdempotencyKey = key, WorkforceRevision = account.WorkforceRevision, CreatedAt = DateTimeOffset.UtcNow, CreatedByAccountId = command.ActorAccountId }, cancellationToken);
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
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Staff session revocation is pending reconciliation for account {AccountId}.", persisted.Account!.Id);
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

public sealed class SendStaffWorkforceInvitationCommandHandler(IStaffWorkforceStore accounts, AccountInvitationService invitations)
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
