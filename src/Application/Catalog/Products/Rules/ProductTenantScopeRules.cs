using Domain.Tenants.Enums;

namespace Application.Catalog.Products.Rules;

internal static class ProductTenantScopeRules
{
    public static string? ValidateTenantScope(TenantScopeType scopeType, Guid? organizationId, Guid? storeId, Guid? kioskId)
    {
        return scopeType switch
        {
            TenantScopeType.Global when organizationId is not null || storeId is not null || kioskId is not null =>
                "Global product cannot be assigned to organization, store, or kiosk.",
            TenantScopeType.Organization when organizationId is null || storeId is not null || kioskId is not null =>
                "Organization-scoped product requires organizationId only.",
            TenantScopeType.Store when organizationId is null || storeId is null || kioskId is not null =>
                "Store-scoped product requires organizationId and storeId only.",
            TenantScopeType.Kiosk when organizationId is null || storeId is null || kioskId is null =>
                "Kiosk-scoped product requires organizationId, storeId, and kioskId.",
            TenantScopeType.Device => "Device-scoped product is not supported.",
            _ => null
        };
    }
}
