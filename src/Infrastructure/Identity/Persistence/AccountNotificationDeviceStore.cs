using Application.Identity.NotificationDevices.Abstractions;
using Domain.Identity.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Identity.Persistence;

public sealed class AccountNotificationDeviceStore : IAccountNotificationDeviceStore
{
    private readonly IceBotDbContext _dbContext;

    public AccountNotificationDeviceStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AccountNotificationDevice?> GetByAccountAndInstallationAsync(
        Guid accountId,
        Guid installationId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AccountNotificationDevices
            .Where(device => device.AccountId == accountId && device.InstallationId == installationId);

        return (asNoTracking ? query.AsNoTracking() : query)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<AccountNotificationDevice?> GetActiveByPushTokenHashAsync(
        string pushTokenHash,
        CancellationToken cancellationToken = default) =>
        _dbContext.AccountNotificationDevices
            .SingleOrDefaultAsync(
                device => device.PushTokenHash == pushTokenHash && device.InvalidatedAt == null,
                cancellationToken);

    public async Task<IReadOnlyList<AccountNotificationDevice>> ListByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.AccountNotificationDevices
            .AsNoTracking()
            .Where(device => device.AccountId == accountId)
            .OrderByDescending(device => device.InvalidatedAt == null)
            .ThenByDescending(device => device.LastSeenAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AccountNotificationDevice>> ListActiveByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.AccountNotificationDevices
            .Where(device =>
                device.AccountId == accountId &&
                device.InvalidatedAt == null &&
                device.PushToken != null)
            .OrderBy(device => device.Id)
            .ToListAsync(cancellationToken);

    public Task AddAsync(AccountNotificationDevice device, CancellationToken cancellationToken = default) =>
        _dbContext.AccountNotificationDevices.AddAsync(device, cancellationToken).AsTask();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteRegistrationTransactionAsync<T>(
        string pushTokenHash,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({pushTokenHash}))",
                cancellationToken);

            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
