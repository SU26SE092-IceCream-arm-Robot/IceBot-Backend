using Domain.Tenants.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Tenants.Abstractions;

public interface IOrganizationStore
{
    Task<Organization?> GetByIdAsync(Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default);
    
    Task<Organization?> GetByCodeAsync(string code, bool asNoTracking = true, CancellationToken cancellationToken = default);
    
    Task<List<Organization>> ListAsync(string? search, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    Task<List<Organization>> ListByIdsAsync(IEnumerable<Guid> ids, string? search, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    Task<int> CountAsync(string? search, string? status, CancellationToken cancellationToken = default);
    
    Task<int> CountByIdsAsync(IEnumerable<Guid> ids, string? search, string? status, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    
    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
