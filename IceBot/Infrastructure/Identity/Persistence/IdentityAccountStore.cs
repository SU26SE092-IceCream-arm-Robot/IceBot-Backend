using Application.Identity.Abstractions;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity.Persistence
{
    public class IdentityAccountStore : IIdentityAccountStore
    {
        private readonly IceBotDbContext _dbContext;

        public IdentityAccountStore(IceBotDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Account?> GetByIdAsync(Guid accountId, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            return BuildAccountQuery(asNoTracking)
                .FirstOrDefaultAsync(account => account.Id == accountId, cancellationToken);
        }

        public Task<Account?> GetByEmailOrUserNameAsync(string emailOrUserName, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            return BuildAccountQuery(asNoTracking)
                .FirstOrDefaultAsync(
                    account => account.Email == emailOrUserName || account.UserName == emailOrUserName,
                    cancellationToken);
        }

        public Task<Account?> GetByGoogleSubjectIdAsync(string googleSubjectId, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            return BuildAccountQuery(asNoTracking)
                .FirstOrDefaultAsync(
                    account => account.GoogleSubjectId == googleSubjectId,
                    cancellationToken);
        }

        public Task<Role?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return _dbContext.Roles.FirstOrDefaultAsync(role => role.Code == code && role.IsActive, cancellationToken);
        }

        public Task<List<Account>> ListAsync(string? search, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            return ApplyFilters(BuildAccountQuery(asNoTracking: true), search, status)
                .OrderBy(account => account.UserName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountAsync(string? search, string? status, CancellationToken cancellationToken = default)
        {
            return ApplyFilters(_dbContext.Accounts.AsNoTracking(), search, status)
                .CountAsync(cancellationToken);
        }

        public Task<bool> ExistsByEmailOrUserNameAsync(string email, string userName, CancellationToken cancellationToken = default)
        {
            return _dbContext.Accounts
                .AsNoTracking()
                .AnyAsync(account => account.Email == email || account.UserName == userName, cancellationToken);
        }

        public Task<bool> EmailExistsForOtherAccountAsync(Guid accountId, string email, CancellationToken cancellationToken = default)
        {
            return _dbContext.Accounts
                .AsNoTracking()
                .AnyAsync(account => account.Id != accountId && account.Email == email, cancellationToken);
        }

        public Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken = default)
        {
            return _dbContext.Accounts
                .AsNoTracking()
                .AnyAsync(account => account.UserName == userName, cancellationToken);
        }

        public Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            return _dbContext.Accounts.AddAsync(account, cancellationToken).AsTask();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        private IQueryable<Account> BuildAccountQuery(bool asNoTracking)
        {
            var query = _dbContext.Accounts
                .Include(account => account.AccountRoles)
                    .ThenInclude(accountRole => accountRole.Role)
                .AsQueryable();

            return asNoTracking ? query.AsNoTracking() : query;
        }

        private static IQueryable<Account> ApplyFilters(IQueryable<Account> query, string? search, string? status)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalized = search.Trim().ToLowerInvariant();
                query = query.Where(account =>
                    account.UserName.ToLower().Contains(normalized) ||
                    account.Email.ToLower().Contains(normalized) ||
                    (account.FullName != null && account.FullName.ToLower().Contains(normalized)));
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<AccountStatus>(status.Trim(), ignoreCase: true, out var parsedStatus))
            {
                query = query.Where(account => account.Status == parsedStatus);
            }

            return query;
        }
    }
}
