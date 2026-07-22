using Domain.Operations.Entities;

namespace Application.Operations.Alerts.Notifications;

public interface INotificationDeliveryStore
{
    Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default);
    Task<bool> ExistsByKeyAsync(string deliveryKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> ListDueIdsAsync(
        DateTimeOffset now,
        DateTimeOffset processingStartedBefore,
        int batchSize,
        CancellationToken cancellationToken = default);
    Task<NotificationDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NotificationDelivery?> GetByOrganizationAsync(Guid organizationId, Guid id,
        CancellationToken cancellationToken = default);
    Task AcquireLockAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
