using Application.Operations.Alerts.Notifications;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Operations.Persistence;

public sealed class NotificationDeliveryStore(IceBotDbContext db) : INotificationDeliveryStore
{
    public Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default) =>
        db.NotificationDeliveries.AddAsync(delivery, cancellationToken).AsTask();

    public Task<bool> ExistsByKeyAsync(string deliveryKey, CancellationToken cancellationToken = default) =>
        db.NotificationDeliveries.AsNoTracking()
            .AnyAsync(delivery => delivery.DeliveryKey == deliveryKey, cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListDueIdsAsync(
        DateTimeOffset now,
        DateTimeOffset processingStartedBefore,
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await db.NotificationDeliveries.AsNoTracking()
            .Where(delivery =>
                (delivery.AttemptCount < delivery.MaxAttempts &&
                 ((delivery.Status == NotificationDeliveryStatus.Pending && delivery.NextAttemptAt <= now) ||
                  (delivery.Status == NotificationDeliveryStatus.Failed && delivery.NextAttemptAt <= now))) ||
                (delivery.Status == NotificationDeliveryStatus.Processing &&
                 delivery.ProcessingStartedAt <= processingStartedBefore))
            .OrderBy(delivery => delivery.NextAttemptAt)
            .ThenBy(delivery => delivery.Id)
            .Select(delivery => delivery.Id)
            .Take(Math.Clamp(batchSize, 1, 500))
            .ToListAsync(cancellationToken);

    public Task<NotificationDelivery?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.NotificationDeliveries.FirstOrDefaultAsync(delivery => delivery.Id == id, cancellationToken);

    public Task<NotificationDelivery?> GetByOrganizationAsync(Guid organizationId, Guid id,
        CancellationToken cancellationToken = default) =>
        db.NotificationDeliveries.FirstOrDefaultAsync(
            delivery => delivery.OrganizationId == organizationId && delivery.Id == id,
            cancellationToken);

    public Task AcquireLockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lockKey = $"notification-delivery:{id:N}";
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));",
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
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
