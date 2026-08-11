using Application.Identity.Access.Results;
using Application.Identity.InternalAccounts;
using Application.Identity.Roles.Queries;
using Application.Identity.Tokens.Claims;
using Domain.Identity.Entities;

namespace Application.Identity.Access.Mapping;

internal static class AccountAccessResultMapper
{
    public static AccountAccessResult FromAccount(Account account, Guid? organizationId = null)
    {
        var activeRoles = account.AccountRoles
            .Where(accountRole => accountRole.IsActive &&
                (!organizationId.HasValue || AccountManagementAccessRules.BelongsToOrganization(accountRole, organizationId.Value)))
            .ToList();

        var roleScopes = activeRoles
            .Select(accountRole => new AccountRoleScopeAccessResult
            {
                RoleCode = accountRole.Role.Code,
                OrganizationId = accountRole.OrganizationId,
                StoreId = accountRole.StoreId,
                KioskId = accountRole.KioskId
            })
            .ToList();

        var roleCodes = activeRoles
            .Select(accountRole => accountRole.Role.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(roleCode => roleCode)
            .ToList();
        var isSystemAdmin = roleCodes.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase);

        return new AccountAccessResult
        {
            AccountId = account.Id,
            IsSystemAdmin = isSystemAdmin,
            Roles = roleCodes,
            PermissionCodes = PermissionCatalog.ResolvePermissionCodes(roleCodes),
            PermissionScopes = ToPermissionScopes(roleCodes, roleScopes, isSystemAdmin),
            RoleScopes = roleScopes,
            EffectiveScope = ToEffectiveScope(roleScopes)
        };
    }

    public static AccountAccessResult FromCurrentUserContext(
        CurrentUserContext userContext,
        IReadOnlyCollection<string> roleCodes,
        IReadOnlyCollection<string> roleScopeClaims)
    {
        var roleScopes = roleScopeClaims
            .Select(ParseRoleScopeClaim)
            .Where(roleScope => roleScope is not null)
            .Cast<AccountRoleScopeAccessResult>()
            .ToList();

        var normalizedRoleCodes = roleCodes
            .Where(roleCode => !string.IsNullOrWhiteSpace(roleCode))
            .Append(userContext.IsSystemAdmin ? "SystemAdmin" : string.Empty)
            .Where(roleCode => !string.IsNullOrWhiteSpace(roleCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(roleCode => roleCode)
            .ToList();

        return new AccountAccessResult
        {
            AccountId = userContext.AccountId,
            IsSystemAdmin = userContext.IsSystemAdmin,
            Roles = normalizedRoleCodes,
            PermissionCodes = PermissionCatalog.ResolvePermissionCodes(normalizedRoleCodes),
            PermissionScopes = ToPermissionScopes(
                normalizedRoleCodes,
                roleScopes,
                userContext.IsSystemAdmin),
            RoleScopes = roleScopes,
            EffectiveScope = new EffectiveScopeResult
            {
                OrganizationIds = userContext.AllowedOrganizationIds.OrderBy(id => id).ToList(),
                StoreIds = userContext.AllowedStoreIds.OrderBy(id => id).ToList(),
                KioskIds = userContext.AllowedKioskIds.OrderBy(id => id).ToList()
            }
        };
    }

    private static List<PermissionScopeAccessResult> ToPermissionScopes(
        IReadOnlyCollection<string> roleCodes,
        IReadOnlyCollection<AccountRoleScopeAccessResult> roleScopes,
        bool isSystemAdmin)
    {
        var activeRoleCodes = roleCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return PermissionCatalog.Matrix
            .Where(permission => permission.Roles.Any(activeRoleCodes.Contains))
            .OrderBy(permission => permission.Policy, StringComparer.Ordinal)
            .Select(permission =>
            {
                var grantingRoles = permission.Roles
                    .Where(activeRoleCodes.Contains)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var matchingScopes = roleScopes
                    .Where(roleScope => grantingRoles.Contains(roleScope.RoleCode))
                    .Select(roleScope => new AccessScopeResult
                    {
                        OrganizationId = roleScope.OrganizationId,
                        StoreId = roleScope.StoreId,
                        KioskId = roleScope.KioskId
                    })
                    .DistinctBy(scope => (scope.OrganizationId, scope.StoreId, scope.KioskId))
                    .OrderBy(scope => scope.OrganizationId)
                    .ThenBy(scope => scope.StoreId)
                    .ThenBy(scope => scope.KioskId)
                    .ToList();

                return new PermissionScopeAccessResult
                {
                    PermissionCode = permission.Policy,
                    ScopeRequired = permission.ScopeRequired,
                    IsGlobal = isSystemAdmin ||
                        !permission.ScopeRequired ||
                        matchingScopes.Any(scope =>
                            !scope.OrganizationId.HasValue &&
                            !scope.StoreId.HasValue &&
                            !scope.KioskId.HasValue),
                    Scopes = matchingScopes
                };
            })
            .ToList();
    }

    private static EffectiveScopeResult ToEffectiveScope(IReadOnlyCollection<AccountRoleScopeAccessResult> roleScopes)
    {
        return new EffectiveScopeResult
        {
            OrganizationIds = roleScopes
                .Where(roleScope => roleScope.OrganizationId.HasValue && !roleScope.StoreId.HasValue && !roleScope.KioskId.HasValue)
                .Select(roleScope => roleScope.OrganizationId!.Value)
                .Distinct()
                .OrderBy(id => id)
                .ToList(),
            StoreIds = roleScopes
                .Where(roleScope => roleScope.StoreId.HasValue && !roleScope.KioskId.HasValue)
                .Select(roleScope => roleScope.StoreId!.Value)
                .Distinct()
                .OrderBy(id => id)
                .ToList(),
            KioskIds = roleScopes
                .Where(roleScope => roleScope.KioskId.HasValue)
                .Select(roleScope => roleScope.KioskId!.Value)
                .Distinct()
                .OrderBy(id => id)
                .ToList()
        };
    }

    private static AccountRoleScopeAccessResult? ParseRoleScopeClaim(string claimValue)
    {
        var parts = claimValue.Split('|');
        if (parts.Length != 4)
        {
            return null;
        }

        return new AccountRoleScopeAccessResult
        {
            RoleCode = parts[0],
            OrganizationId = ParseScopeId(parts[1]),
            StoreId = ParseScopeId(parts[2]),
            KioskId = ParseScopeId(parts[3])
        };
    }

    private static Guid? ParseScopeId(string value)
    {
        return value != "*" && Guid.TryParse(value, out var parsed)
            ? parsed
            : null;
    }
}
