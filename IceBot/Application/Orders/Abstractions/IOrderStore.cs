using Domain.Orders.Entities;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;

namespace Application.Orders.Abstractions;

public interface IOrderStore
{
    Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<MenuItem?> GetMenuItemByIdAsync(Guid menuItemId, CancellationToken cancellationToken = default);

    Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<Order?> GetOrderByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<Order?> GetOrderByClientOrderIdAsync(Guid kioskId, string clientOrderId, CancellationToken cancellationToken = default);

    Task AddOrderAsync(Order order, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
