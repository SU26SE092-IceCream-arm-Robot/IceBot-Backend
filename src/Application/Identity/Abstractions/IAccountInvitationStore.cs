using Domain.Identity.Entities;

namespace Application.Identity.Abstractions;

public interface IAccountInvitationStore
{
    Task<AccountInvitation?> GetByTokenHashAsync(
        string tokenHash,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<List<AccountInvitation>> GetActiveInvitationsByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task AddAsync(AccountInvitation invitation, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteCreationTransactionAsync<T>(
        Guid accountId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteAcceptanceTransactionAsync<T>(
        string tokenHash,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
