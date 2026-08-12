using Application.Tenants.Abstractions;
using Domain.Common.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tenants.Persistence;

public sealed class OrganizationAccessStateReader : IOrganizationAccessStateReader
{
    private readonly IceBotDbContext _dbContext;

    public OrganizationAccessStateReader(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsActiveAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        _dbContext.Organizations.WhereNotDeleted()
            .AnyAsync(
                organization => organization.Id == organizationId && organization.Status == EntityStatus.Active,
                cancellationToken);

    public async Task<IReadOnlySet<OrganizationScopeReference>> FilterActiveScopesAsync(
        IReadOnlyCollection<OrganizationScopeReference> scopes,
        CancellationToken cancellationToken = default)
    {
        if (scopes.Count == 0)
        {
            return new HashSet<OrganizationScopeReference>();
        }

        var organizationIds = scopes
            .Where(scope => scope.OrganizationId.HasValue && !scope.StoreId.HasValue && !scope.KioskId.HasValue)
            .Select(scope => scope.OrganizationId!.Value)
            .Distinct()
            .ToArray();
        var storeIds = scopes.Where(scope => scope.StoreId.HasValue).Select(scope => scope.StoreId!.Value).Distinct().ToArray();
        var kioskIds = scopes.Where(scope => scope.KioskId.HasValue).Select(scope => scope.KioskId!.Value).Distinct().ToArray();

        var activeOrganizationIds = organizationIds.Length == 0
            ? Array.Empty<Guid>()
            : await _dbContext.Organizations.WhereNotDeleted()
                .Where(organization => organizationIds.Contains(organization.Id) && organization.Status == EntityStatus.Active)
                .Select(organization => organization.Id)
                .ToArrayAsync(cancellationToken);

        var activeStoreIds = storeIds.Length == 0
            ? Array.Empty<Guid>()
            : await _dbContext.Stores.WhereNotDeleted()
                .Where(store => storeIds.Contains(store.Id) && store.Organization.Status == EntityStatus.Active)
                .Select(store => store.Id)
                .ToArrayAsync(cancellationToken);

        var activeKioskIds = kioskIds.Length == 0
            ? Array.Empty<Guid>()
            : await _dbContext.Kiosks.WhereNotDeleted()
                .Where(kiosk => kioskIds.Contains(kiosk.Id) && kiosk.Organization.Status == EntityStatus.Active)
                .Select(kiosk => kiosk.Id)
                .ToArrayAsync(cancellationToken);

        var activeOrganizations = activeOrganizationIds.ToHashSet();
        var activeStores = activeStoreIds.ToHashSet();
        var activeKiosks = activeKioskIds.ToHashSet();

        return scopes
            .Where(scope =>
                scope.KioskId.HasValue
                    ? activeKiosks.Contains(scope.KioskId.Value)
                    : scope.StoreId.HasValue
                        ? activeStores.Contains(scope.StoreId.Value)
                        : scope.OrganizationId.HasValue && activeOrganizations.Contains(scope.OrganizationId.Value))
            .ToHashSet();
    }
}
