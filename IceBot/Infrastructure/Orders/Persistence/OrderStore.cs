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
        return _dbContext.Kiosks.FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);
    }

    public Task<MenuItem?> GetMenuItemByIdAsync(Guid menuItemId, CancellationToken cancellationToken = default)
    {
        return _dbContext.MenuItems
            .Include(menuItem => menuItem.Menu)
            .Include(menuItem => menuItem.Product)
            .Include(menuItem => menuItem.Recipe)
            .FirstOrDefaultAsync(menuItem => menuItem.Id == menuItemId, cancellationToken);
    }

    public Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
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
