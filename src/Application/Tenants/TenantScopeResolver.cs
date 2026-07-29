using Domain.Tenants.Enums;

namespace Application.Tenants;

public static class TenantScopeResolver
{
    public static TenantScopeType Resolve(Guid? storeId, Guid? kioskId, Guid? deviceId = null)
    {
        if (deviceId.HasValue) return TenantScopeType.Device;
        if (kioskId.HasValue) return TenantScopeType.Kiosk;
        if (storeId.HasValue) return TenantScopeType.Store;
        return TenantScopeType.Organization;
    }
}
