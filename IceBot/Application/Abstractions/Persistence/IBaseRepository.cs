using System.Linq.Expressions;

namespace Application.Abstractions.Persistence;

public interface IBaseRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> Query(bool asNoTracking = true);

    IQueryable<TEntity> QueryIgnoreFilters(bool asNoTracking = true);

    ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? configure = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? configure = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    void RemoveRange(IEnumerable<TEntity> entities);

    void SoftDelete(TEntity entity, Guid? deletedByAccountId = null);
}
