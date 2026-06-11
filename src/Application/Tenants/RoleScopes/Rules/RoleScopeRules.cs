using Domain.Tenants.Enums;

namespace Application.Tenants.RoleScopes.Rules;

internal static class RoleScopeRules
{
    public static readonly Dictionary<string, (TenantScopeType[] AllowedScopes, bool RequiresScope)> ScopeMetadata = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SystemAdmin"] = (new[] { TenantScopeType.Global }, false),
        ["OrgAdmin"] = (new[] { TenantScopeType.Organization }, true),
        ["Manager"] = (new[] { TenantScopeType.Organization, TenantScopeType.Store }, true),
        ["Technician"] = (new[] { TenantScopeType.Store, TenantScopeType.Kiosk }, true),
        ["Staff"] = (new[] { TenantScopeType.Store, TenantScopeType.Kiosk }, true)
    };
}
