using Application.Orders.Abstractions;
using Application.Orders.Management.Results;
using Domain.Orders.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Orders.Persistence;

public sealed class OrderStore : IOrderStore
{
    private readonly IceBotDbContext _dbContext;

    public OrderStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

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
        var query = _dbContext.Orders.AsNoTracking();

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

        var statusCounts = await query
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byStatus = statusCounts
            .Select(sc => new OrderStatusSummaryDto
            {
                Status = sc.Status.ToString(),
                Count = sc.Count
            })
            .ToList();

        var recentOrdersList = await query
            .Include(o => o.Kiosk)
            .OrderByDescending(o => o.PlacedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        var recentOrders = recentOrdersList.Select(o =>
        {
            var project = Application.Shared.Utils.OrderStatusProjector.ProjectFromOrder(o);
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
                CustomerStatus = project.CustomerStatus,
                CustomerStatusMessage = project.CustomerStatusMessage,
                RequiresStaffSupport = project.RequiresStaffSupport
            };
        }).ToList();

        return new OrderOverviewResult
        {
            TotalCount = totalCount,
            ByStatus = byStatus,
            RecentOrders = recentOrders
        };
    }

    public Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks
            .Include(kiosk => kiosk.Store)
            .Include(kiosk => kiosk.Organization)
            .FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);
    }

    public Task<MenuItem?> GetMenuItemByIdAsync(Guid menuItemId, CancellationToken cancellationToken = default)
    {
        return _dbContext.MenuItems
            .Include(menuItem => menuItem.Menu)
            .Include(menuItem => menuItem.Product)
            .Include(menuItem => menuItem.ProductVariant)
            .Include(menuItem => menuItem.Recipe)
            .FirstOrDefaultAsync(menuItem => menuItem.Id == menuItemId, cancellationToken);
    }

    public Task<bool> HasActiveProductionRouteAsync(
        Guid kioskId,
        Guid productVariantId,
        Guid recipeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskConfigurationDeployments
            .AsNoTracking()
            .AnyAsync(deployment =>
                deployment.KioskId == kioskId &&
                deployment.Status == KioskConfigurationDeploymentStatus.Active &&
                deployment.ConfigurationRelease.Status == ConfigurationReleaseStatus.Published &&
                deployment.ConfigurationRelease.ExecutionRoutes.Any(route =>
                    route.ProductVariantId == productVariantId &&
                    route.RecipeId == recipeId &&
                    route.RobotBindings.Any()),
                cancellationToken);
    }

    public Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public Task<int> CountOrdersAsync(
        string? search,
        Domain.Orders.Enums.OrderStatus? status,
        Domain.Orders.Enums.PaymentStatus? paymentStatus,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFiltersAndScope(
            search,
            status,
            paymentStatus,
            organizationId,
            storeId,
            kioskId,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds);

        return query.CountAsync(cancellationToken);
    }

    public Task<List<Order>> ListOrdersAsync(
        string? search,
        Domain.Orders.Enums.OrderStatus? status,
        Domain.Orders.Enums.PaymentStatus? paymentStatus,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFiltersAndScope(
            search,
            status,
            paymentStatus,
            organizationId,
            storeId,
            kioskId,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds);

        return query
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.PlacedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountOrderStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderStatusHistories
            .AsNoTracking()
            .CountAsync(history => history.OrderId == orderId, cancellationToken);
    }

    public Task<List<OrderStatusHistory>> ListOrderStatusHistoryAsync(
        Guid orderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderStatusHistories
            .AsNoTracking()
            .Include(history => history.ChangedByAccount)
            .Where(history => history.OrderId == orderId)
            .OrderByDescending(history => history.ChangedAt)
            .ThenByDescending(history => history.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Order> ApplyFiltersAndScope(
        string? search,
        Domain.Orders.Enums.OrderStatus? status,
        Domain.Orders.Enums.PaymentStatus? paymentStatus,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds)
    {
        var query = _dbContext.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(searchLower) ||
                (o.CustomerName != null && o.CustomerName.ToLower().Contains(searchLower)) ||
                (o.CustomerPhoneNumber != null && o.CustomerPhoneNumber.ToLower().Contains(searchLower)));
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (paymentStatus.HasValue)
        {
            query = query.Where(o => o.PaymentStatus == paymentStatus.Value);
        }

        if (organizationId.HasValue)
        {
            query = query.Where(o => o.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(o => o.StoreId == storeId.Value);
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

        return query;
    }

    public Task<Order?> GetOrderByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(order => order.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public Task<Order?> GetOrderByClientOrderIdAsync(Guid kioskId, string clientOrderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(
                order => order.KioskId == kioskId && order.ClientOrderId == clientOrderId,
                cancellationToken);
    }

    public async Task AddOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public async Task AddOrderStatusHistoryAsync(OrderStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _dbContext.OrderStatusHistories.AddAsync(history, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
