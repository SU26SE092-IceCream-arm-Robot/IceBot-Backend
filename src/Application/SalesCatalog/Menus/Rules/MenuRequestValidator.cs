using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.Menus.Support;
using Domain.Tenants.Enums;

namespace Application.SalesCatalog.Menus.Rules;

internal static class MenuRequestValidator
{
    public static async Task<string?> ValidateMenuFieldsAsync(
        IMenuStore menus,
        string code,
        string name,
        string currency,
        TenantScopeType scopeType,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        Guid? excludedMenuId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code)) return "Menu code is required.";
        if (string.IsNullOrWhiteSpace(name)) return "Menu name is required.";
        if (string.IsNullOrWhiteSpace(currency)) return "Currency is required.";
        if (effectiveFrom is not null && effectiveTo is not null && effectiveFrom > effectiveTo) return "Menu effectiveFrom cannot be after effectiveTo.";

        var scopeError = MenuTenantScopeRules.ValidateTenantScope(scopeType, organizationId, storeId, kioskId);
        if (scopeError is not null) return scopeError;

        if (await menus.MenuCodeExistsAsync(organizationId, storeId, kioskId, MenuNormalizer.NormalizeCode(code), excludedMenuId, cancellationToken))
        {
            return "Menu code already exists in this scope.";
        }

        return null;
    }
}
