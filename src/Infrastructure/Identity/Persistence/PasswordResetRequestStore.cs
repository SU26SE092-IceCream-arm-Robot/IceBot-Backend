using Application.Identity.Abstractions;
using Domain.Identity.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity.Persistence;

public sealed class PasswordResetRequestStore : IPasswordResetRequestStore
{
    private readonly IceBotDbContext _dbContext;

    public PasswordResetRequestStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PasswordResetRequest?> GetByTokenHashAsync(
        string tokenHash,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PasswordResetRequests
            .Include(passwordResetRequest => passwordResetRequest.Account)
            .Where(passwordResetRequest => passwordResetRequest.TokenHash == tokenHash);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(PasswordResetRequest passwordResetRequest, CancellationToken cancellationToken = default)
    {
        return _dbContext.PasswordResetRequests.AddAsync(passwordResetRequest, cancellationToken).AsTask();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
