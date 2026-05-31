using Application.Abstractions.Persistence;
using Domain.Common;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Persistence.Repositories;

public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    protected readonly IceBotDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public BaseRepository(IceBotDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> Query(bool asNoTracking = true)
    {
        return ApplyTracking(DbSet, asNoTracking);
    }

    public IQueryable<TEntity> QueryIgnoreFilters(bool asNoTracking = true)
    {
        return ApplyTracking(DbSet.IgnoreQueryFilters(), asNoTracking);
    }

    public ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default)
    {
        if (keyValues.Length == 0)
        {
            throw new ArgumentException("At least one key value is required.", nameof(keyValues));
        }

        return DbSet.FindAsync(keyValues, cancellationToken);
    }

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? configure = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(predicate, configure, asNoTracking);
        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? configure = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(predicate, configure, asNoTracking);
        return query.ToListAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(predicate, cancellationToken);
    }

    public Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        return predicate is null
            ? DbSet.CountAsync(cancellationToken)
            : DbSet.CountAsync(predicate, cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        StampCreated(entity);
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();

        foreach (var entity in entityList)
        {
            StampCreated(entity);
        }

        return DbSet.AddRangeAsync(entityList, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        StampUpdated(entity);
        DbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
    }

    public void SoftDelete(TEntity entity, Guid? deletedByAccountId = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is not ISoftDeletable softDeletable)
        {
            throw new InvalidOperationException($"{typeof(TEntity).Name} does not support soft delete.");
        }

        softDeletable.DeletedAt ??= DateTimeOffset.UtcNow;
        softDeletable.DeletedByAccountId ??= deletedByAccountId;
        StampUpdated(entity);
        DbSet.Update(entity);
    }

    private static IQueryable<TEntity> ApplyTracking(IQueryable<TEntity> query, bool asNoTracking)
    {
        return asNoTracking ? query.AsNoTracking() : query;
    }

    private static void StampCreated(TEntity entity)
    {
        if (entity is IAuditable auditable && auditable.CreatedAt == default)
        {
            auditable.CreatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static void StampUpdated(TEntity entity)
    {
        if (entity is IAuditable auditable)
        {
            auditable.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private IQueryable<TEntity> BuildQuery(
        Expression<Func<TEntity, bool>>? predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? configure,
        bool asNoTracking)
    {
        var query = Query(asNoTracking);

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return configure is null ? query : configure(query);
    }
}
