using Application.Identity.Abstractions;
using Domain.Identity.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Identity.Persistence;

public sealed class AccountInvitationStore : IAccountInvitationStore
{
    private readonly IceBotDbContext _dbContext;

    public AccountInvitationStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AccountInvitation?> GetByTokenHashAsync(
        string tokenHash,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AccountInvitations
            .Include(invitation => invitation.Account)
            .Where(invitation => invitation.TokenHash == tokenHash);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<AccountInvitation>> GetActiveInvitationsByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.AccountInvitations
            .Where(x => x.AccountId == accountId && x.AcceptedAt == null && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(AccountInvitation invitation, CancellationToken cancellationToken = default)
    {
        return _dbContext.AccountInvitations.AddAsync(invitation, cancellationToken).AsTask();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<T> ExecuteCreationTransactionAsync<T>(
        Guid accountId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        ExecuteSerializedAsync($"account-invitation:create:{accountId:D}", action, cancellationToken);

    public Task<T> ExecuteAcceptanceTransactionAsync<T>(
        string tokenHash,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        ExecuteSerializedAsync($"account-invitation:accept:{tokenHash}", action, cancellationToken);

    private async Task<T> ExecuteSerializedAsync<T>(
        string lockKey,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.CurrentTransaction is not null)
            return await action(cancellationToken);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
                cancellationToken);
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
