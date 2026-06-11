using Application.Orders.Abstractions;
using Domain.Orders.Entities;
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
