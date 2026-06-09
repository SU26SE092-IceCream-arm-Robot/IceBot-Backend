using Application.Tenants.RoleScopes.Results;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Tenants.RoleScopes.Mapping;

internal static class RoleScopeOptionsResultMapper
{
    public static RoleScopeOptionsResult ToResult(
        string roleCode,
        TenantScopeType[] allowedScopes,
        bool requiresScope,
        IReadOnlyList<Organization> organizations,
        IReadOnlyList<Store> stores,
        IReadOnlyList<Kiosk> kiosks)
    {
        var orgResults = new List<RoleScopeOrganizationResult>();

        if (requiresScope)
        {
            var allowedScopeSet = allowedScopes.ToHashSet();
            var canSelectStore = allowedScopeSet.Contains(TenantScopeType.Store) || allowedScopeSet.Contains(TenantScopeType.Kiosk);
            var canSelectKiosk = allowedScopeSet.Contains(TenantScopeType.Kiosk);

            var kiosksByStore = kiosks
                .GroupBy(k => k.StoreId)
                .ToDictionary(g => g.Key, g => g.OrderBy(k => k.Code).ToList());

            var storesByOrg = stores
                .GroupBy(s => s.OrganizationId)
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Code).ToList());

            foreach (var org in organizations.OrderBy(o => o.Code))
            {
                var storeResults = new List<RoleScopeStoreResult>();

                if (canSelectStore && storesByOrg.TryGetValue(org.Id, out var orgStores))
                {
                    foreach (var store in orgStores)
                    {
                        var kioskResults = new List<RoleScopeKioskResult>();

                        if (canSelectKiosk && kiosksByStore.TryGetValue(store.Id, out var storeKiosks))
                        {
                            foreach (var kiosk in storeKiosks)
                            {
                                kioskResults.Add(new RoleScopeKioskResult
                                {
                                    Id = kiosk.Id,
                                    OrganizationId = kiosk.OrganizationId,
                                    StoreId = kiosk.StoreId,
                                    Code = kiosk.Code,
                                    Name = kiosk.Name
                                });
                            }
                        }

                        storeResults.Add(new RoleScopeStoreResult
                        {
                            Id = store.Id,
                            OrganizationId = store.OrganizationId,
                            Code = store.Code,
                            Name = store.Name,
                            Kiosks = kioskResults
                        });
                    }
                }

                orgResults.Add(new RoleScopeOrganizationResult
                {
                    Id = org.Id,
                    Code = org.Code,
                    Name = org.Name,
                    Stores = storeResults
                });
            }
        }

        return new RoleScopeOptionsResult
        {
            RoleCode = roleCode,
            AllowedScopeTypes = allowedScopes.Select(s => s.ToString()).ToList(),
            RequiresScope = requiresScope,
            Organizations = orgResults
        };
    }
}
