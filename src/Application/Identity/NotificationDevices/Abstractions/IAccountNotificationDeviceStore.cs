using Domain.Identity.Entities;

namespace Application.Identity.NotificationDevices.Abstractions;

public interface IAccountNotificationDeviceStore
{
    Task<AccountNotificationDevice?> GetByAccountAndInstallationAsync(
        Guid accountId,
        Guid installationId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<AccountNotificationDevice?> GetActiveByPushTokenHashAsync(
        string pushTokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountNotificationDevice>> ListByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task AddAsync(AccountNotificationDevice device, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteRegistrationTransactionAsync<T>(
        string pushTokenHash,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
