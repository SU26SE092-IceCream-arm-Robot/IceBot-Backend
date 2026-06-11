using Domain.Tenants.Enums;

namespace Application.SalesCatalog.Menus.Rules;

internal static class MenuTenantScopeRules
{
    public static string? ValidateTenantScope(TenantScopeType scopeType, Guid? organizationId, Guid? storeId, Guid? kioskId)
    {
        return scopeType switch
        {
            TenantScopeType.Global when organizationId is not null || storeId is not null || kioskId is not null =>
                "Global menu cannot be assigned to organization, store, or kiosk.",
            TenantScopeType.Organization when organizationId is null || storeId is not null || kioskId is not null =>
                "Organization-scoped menu requires organizationId only.",
            TenantScopeType.Store when organizationId is null || storeId is null || kioskId is not null =>
                "Store-scoped menu requires organizationId and storeId only.",
            TenantScopeType.Kiosk when organizationId is null || storeId is null || kioskId is null =>
                "Kiosk-scoped menu requires organizationId, storeId, and kioskId.",
            TenantScopeType.Device => "Device-scoped menu is not supported.",
            _ => null
        };
    }
}
