using Domain.Common.Enums;
using Domain.Tenants.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Tenants.Abstractions;

public interface IStoreStore
{
    Task<bool> OrganizationExistsActiveAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> StoreCodeExistsAsync(Guid organizationId, string code, Guid? excludeStoreId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Store>> ListAsync(Guid? organizationId, EntityStatus? status, string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Store>> ListByOrganizationIdsAsync(IEnumerable<Guid> organizationIds, EntityStatus? status, string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Store>> ListAccessibleAsync(IEnumerable<Guid> organizationIds, IEnumerable<Guid> storeIds, Guid? organizationId, EntityStatus? status, string? search, CancellationToken cancellationToken = default);
    Task<Store?> GetByIdAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task AddAsync(Store store, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
