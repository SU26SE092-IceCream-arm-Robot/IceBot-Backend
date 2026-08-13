using Application.Identity.Tokens.Claims;
using Application.Identity.Invitations.Results;
using Application.Tenants;
using Application.Tenants.Abstractions;
using Domain.Identity.Entities;
using System.Security.Cryptography;
using System.Text;

namespace Application.Identity.Workforce.Staff;

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
        ITenantTreeStore tenantTree,
        CurrentUserContext user,
        Guid organizationId,
        IReadOnlyList<StaffWorkforceScopeRequest>? scopes,
        CancellationToken cancellationToken)
    {
        scopes ??= [];
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

    public static string CreateFingerprint(CreateStaffWorkforceRequest request) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", request.UserName.Trim().ToUpperInvariant(), request.Email.Trim().ToUpperInvariant(), request.FullName?.Trim(), request.PhoneNumber?.Trim(), request.LocalLoginEnabled, request.GoogleLoginEnabled, request.GoogleEmail?.Trim().ToUpperInvariant(), string.Join(",", (request.StaffScopes ?? []).OrderBy(x => x.StoreId).ThenBy(x => x.KioskId).Select(x => $"{x.StoreId:N}:{x.KioskId:N}"))))));
}
