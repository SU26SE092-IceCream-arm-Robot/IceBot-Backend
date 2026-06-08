using Application.Identity.Abstractions;
using Domain.Identity.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
}
