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
}
