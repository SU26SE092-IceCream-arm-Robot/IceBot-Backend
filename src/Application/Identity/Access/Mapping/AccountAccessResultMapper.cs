using Application.Identity.Access.Results;
using Application.Identity.Tokens.Claims;
using Domain.Identity.Entities;

namespace Application.Identity.Access.Mapping;

internal static class AccountAccessResultMapper
{
    public static AccountAccessResult FromAccount(Account account)
    {
        var activeRoles = account.AccountRoles
            .Where(accountRole => accountRole.IsActive)
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

        return new AccountAccessResult
        {
            AccountId = account.Id,
            IsSystemAdmin = activeRoles.Any(accountRole =>
                string.Equals(accountRole.Role.Code, "SystemAdmin", StringComparison.OrdinalIgnoreCase)),
            Roles = activeRoles
                .Select(accountRole => accountRole.Role.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(roleCode => roleCode)
                .ToList(),
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

        return new AccountAccessResult
        {
            AccountId = userContext.AccountId,
            IsSystemAdmin = userContext.IsSystemAdmin,
            Roles = roleCodes
                .Where(roleCode => !string.IsNullOrWhiteSpace(roleCode))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(roleCode => roleCode)
                .ToList(),
            RoleScopes = roleScopes,
            EffectiveScope = new EffectiveScopeResult
            {
                OrganizationIds = userContext.AllowedOrganizationIds.OrderBy(id => id).ToList(),
                StoreIds = userContext.AllowedStoreIds.OrderBy(id => id).ToList(),
                KioskIds = userContext.AllowedKioskIds.OrderBy(id => id).ToList()
            }
        };
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
