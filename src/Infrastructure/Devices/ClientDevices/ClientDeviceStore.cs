using System.Data;
using Application.ClientDevices.Abstractions;
using Application.Orders.Admission;
using Application.Tenants.Kiosks.Rules;
using Domain.Devices.ClientDevices;
using Domain.Operations.Entities;
using Domain.Tenants.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Devices.ClientDevices;

public sealed class ClientDeviceStore(IceBotDbContext dbContext) : IClientDeviceStore
{
    public Task<Kiosk?> GetKioskAsync(Guid kioskId, CancellationToken cancellationToken = default) =>
        dbContext.Kiosks.WhereNotDeleted()
            .Include(kiosk => kiosk.Organization)
            .Include(kiosk => kiosk.Store)
            .FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);

    public Task<ClientDevice?> GetByIdAsync(Guid clientDeviceId, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ClientDevices
            .Include(device => device.Credentials)
            .AsQueryable();
        if (!tracking)
            query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(device => device.Id == clientDeviceId, cancellationToken);
    }

    public Task<ClientDevice?> GetByInstallationIdAsync(Guid installationId, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ClientDevices
            .Include(device => device.Credentials)
            .Where(device => device.InstallationId == installationId && device.Status != ClientDeviceStatus.Retired);
        if (!tracking)
            query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClientDevice>> ListByKioskAsync(Guid kioskId, CancellationToken cancellationToken = default) =>
        await dbContext.ClientDevices.AsNoTracking()
            .Where(device => device.KioskId == kioskId)
            .OrderByDescending(device => device.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> HasActiveCustomerSessionAsync(Guid kioskId, DateTimeOffset observedAt, CancellationToken cancellationToken = default) =>
        dbContext.Orders.WhereNotDeleted().AnyAsync(
            KioskCustomerSessionAdmission.BuildActiveSessionPredicate(kioskId, observedAt),
            cancellationToken);

    public Task AcquireKioskLockAsync(Guid kioskId, CancellationToken cancellationToken = default) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({KioskOperationalConcurrency.LockKey(kioskId)}, 0));",
            cancellationToken);

    public Task AcquireClientDeviceLockAsync(Guid clientDeviceId, CancellationToken cancellationToken = default) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"client-device:{clientDeviceId:D}"}, 0));",
            cancellationToken);

    public Task<ClientDeviceOperationReplay?> GetReplayAsync(
        Guid kioskId,
        string operation,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.ClientDeviceOperationReplays.FirstOrDefaultAsync(replay =>
            replay.KioskId == kioskId && replay.Operation == operation && replay.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public Task<ClientDeviceOperationReplay?> GetReplayForClientDeviceAsync(
        Guid clientDeviceId,
        string operation,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.ClientDeviceOperationReplays.FirstOrDefaultAsync(replay =>
            replay.ClientDeviceId == clientDeviceId && replay.Operation == operation && replay.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public Task AddClientDeviceAsync(ClientDevice clientDevice, CancellationToken cancellationToken = default) =>
        dbContext.ClientDevices.AddAsync(clientDevice, cancellationToken).AsTask();

    public Task AddReplayAsync(ClientDeviceOperationReplay replay, CancellationToken cancellationToken = default) =>
        dbContext.ClientDeviceOperationReplays.AddAsync(replay, cancellationToken).AsTask();

    public Task AddOperationLogAsync(OperationLog operationLog, CancellationToken cancellationToken = default) =>
        dbContext.OperationLogs.AddAsync(operationLog, cancellationToken).AsTask();

    public Task TryObserveAsync(
        Guid clientDeviceId,
        DateTimeOffset observedAt,
        TimeSpan minimumInterval,
        CancellationToken cancellationToken = default) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"ClientDevices\" SET \"LastSeenAt\" = {observedAt} WHERE \"Id\" = {clientDeviceId} AND (\"LastSeenAt\" IS NULL OR \"LastSeenAt\" <= {observedAt - minimumInterval});",
            cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is not null)
            return await action(cancellationToken);

        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var result = await action(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception exception) when (attempt < maxAttempts && IsTransientSerializationFailure(exception))
            {
                dbContext.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), cancellationToken);
            }
        }
    }

    private static bool IsTransientSerializationFailure(Exception exception) =>
        exception switch
        {
            PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected } => true,
            DbUpdateException { InnerException: Exception innerException } =>
                IsTransientSerializationFailure(innerException),
            _ => exception.InnerException is Exception innerException && IsTransientSerializationFailure(innerException)
        };
}
