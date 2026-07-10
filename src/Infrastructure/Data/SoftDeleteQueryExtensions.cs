using Domain.Common;

namespace Infrastructure.Data;

/// <summary>
/// Makes active-row intent explicit for entities that deliberately do not use an
/// EF global query filter because their required dependents are historical evidence.
/// </summary>
public static class SoftDeleteQueryExtensions
{
    public static IQueryable<TEntity> WhereNotDeleted<TEntity>(this IQueryable<TEntity> query)
        where TEntity : class, ISoftDeletable
    {
        return query.Where(entity => entity.DeletedAt == null);
    }
}
