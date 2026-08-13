using Application.Orders.Management.Results;
using Domain.Orders.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Orders.Persistence;

public sealed partial class OrderStore
{
    public async Task<OrderOverviewResult> GetOrderOverviewAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        Domain.Orders.Enums.OrderStatus? status,
        Guid? kioskId,
        int take,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders.WhereNotDeleted().AsNoTracking();

        if (from.HasValue)
        {
            query = query.Where(o => o.PlacedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(o => o.PlacedAt <= to.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (kioskId.HasValue)
        {
            query = query.Where(o => o.KioskId == kioskId.Value);
        }

        if (!isSystemAdmin)
        {
            var allowedOrgs = allowedOrganizationIds ?? Array.Empty<Guid>();
            var allowedStores = allowedStoreIds ?? Array.Empty<Guid>();
            var allowedKiosks = allowedKioskIds ?? Array.Empty<Guid>();
            query = query.Where(o =>
                (o.OrganizationId.HasValue && allowedOrgs.Contains(o.OrganizationId.Value)) ||
                (o.StoreId.HasValue && allowedStores.Contains(o.StoreId.Value)) ||
                allowedKiosks.Contains(o.KioskId));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var byStatus = await query
            .GroupBy(o => o.Status)
            .Select(g => new OrderStatusSummaryDto { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);
        var recentOrders = (await query
            .Include(o => o.Kiosk)
            .OrderByDescending(o => o.PlacedAt)
            .Take(take)
            .ToListAsync(cancellationToken))
            .Select(o =>
            {
                var projection = Application.Orders.Support.OrderStatusProjector.ProjectFromOrder(o);
                return new RecentOrderDto
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    KioskId = o.KioskId,
                    KioskCode = o.Kiosk?.Code ?? string.Empty,
                    Status = o.Status.ToString(),
                    PaymentStatus = o.PaymentStatus.ToString(),
                    TotalAmount = o.TotalAmount,
                    CreatedAt = o.PlacedAt,
                    CustomerStatus = projection.CustomerStatus,
                    CustomerStatusMessage = projection.CustomerStatusMessage,
                    RequiresStaffSupport = projection.RequiresStaffSupport
                };
            })
            .ToList();

        return new OrderOverviewResult
        {
            TotalCount = totalCount,
            ByStatus = byStatus,
            RecentOrders = recentOrders
        };
    }
}
