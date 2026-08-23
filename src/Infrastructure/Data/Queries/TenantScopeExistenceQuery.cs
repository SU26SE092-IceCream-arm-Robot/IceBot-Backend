using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Queries;

public static class TenantScopeExistenceQuery
{
    public static async Task<bool> ExistsAsync(
        IceBotDbContext dbContext,
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Organizations.WhereNotDeleted()
                .AnyAsync(organization => organization.Id == organizationId, cancellationToken))
        {
            return false;
        }

        if (storeId.HasValue && !await dbContext.Stores.WhereNotDeleted().AnyAsync(
                store => store.Id == storeId.Value && store.OrganizationId == organizationId,
                cancellationToken))
        {
            return false;
        }

        return !kioskId.HasValue || await dbContext.Kiosks.WhereNotDeleted().AnyAsync(
            kiosk => kiosk.Id == kioskId.Value && kiosk.OrganizationId == organizationId &&
                      (!storeId.HasValue || kiosk.StoreId == storeId.Value),
            cancellationToken);
    }
}
