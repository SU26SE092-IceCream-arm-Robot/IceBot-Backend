using Application.Shared.Constants;
using Application.Shared.Wrappers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Shared.Extensions
{
    public static class QueryablePagingExtensions
    {
        public static async Task<PagedResult<TDest>> ToPagedResultAsync<TSource, TDest>(
            this IQueryable<TSource> source,
            int page,
            int pageSize,
            Expression<Func<TSource, TDest>> selector,
            CancellationToken ct = default)
        {
            var (p, ps) = Normalize(page, pageSize);

            var total = await source.CountAsync(ct);
            var data = await source
                .Skip((p - 1) * ps)
                .Take(ps)
                .Select(selector)
                .ToListAsync(ct);

            return PagedResult<TDest>.Success(data, total, p, ps);
        }

        public static async Task<PagedResult<TSource>> ToPagedResultAsync<TSource>(
            this IQueryable<TSource> source,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            var (p, ps) = Normalize(page, pageSize);

            var total = await source.CountAsync(ct);
            var data = await source
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync(ct);

            return PagedResult<TSource>.Success(data, total, p, ps);
        }

        private static (int page, int pageSize) Normalize(int page, int pageSize)
        {
            var p = page < 1 ? PaginationDefaults.DefaultPageNumber : page;
            var ps = pageSize <= 0 || pageSize > PaginationDefaults.MaxPageSize
                ? PaginationDefaults.DefaultPageSize
                : pageSize;
            return (p, ps);
        }
    }
}
