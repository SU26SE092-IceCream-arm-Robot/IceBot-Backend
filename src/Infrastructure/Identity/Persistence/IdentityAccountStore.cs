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

        public async Task<Account?> GetByGoogleEmailAsync(
            string googleEmail,
            bool asNoTracking = true,
            CancellationToken cancellationToken = default)
        {
            var matches = await BuildAccountQuery(asNoTracking)
                .Where(account => account.GoogleEmail == googleEmail)
                .Take(2)
                .ToListAsync(cancellationToken);

            return matches.Count == 1 ? matches[0] : null;
        }

        public Task<Account?> GetByGoogleSubjectIdAsync(string googleSubjectId, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            return BuildAccountQuery(asNoTracking)
                .FirstOrDefaultAsync(
                    account => account.GoogleSubjectId == googleSubjectId,
                    cancellationToken);
        }

        public Task<bool> GoogleEmailExistsAsync(
            string googleEmail,
            Guid? excludedAccountId = null,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.Accounts.AnyAsync(
                account => account.DeletedAt == null &&
                           account.GoogleEmail == googleEmail &&
                           (!excludedAccountId.HasValue || account.Id != excludedAccountId.Value),
                cancellationToken);
        }

        public Task<Role?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return _dbContext.Roles.FirstOrDefaultAsync(role => role.Code == code && role.IsActive, cancellationToken);
        }

        public Task<List<Role>> ListActiveRolesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.Roles
                .AsNoTracking()
                .Where(role => role.IsActive)
                .OrderBy(role => role.Priority)
                .ToListAsync(cancellationToken);
        }

        public Task<List<Account>> ListAsync(
            string? search,
            string? status,
            bool isSystemAdmin,
            IReadOnlySet<Guid> allowedOrganizationIds,
            IReadOnlySet<Guid> allowedStoreIds,
            IReadOnlySet<Guid> allowedKioskIds,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return ApplyFilters(
                    BuildAccountQuery(asNoTracking: true),
                    search,
                    status,
                    isSystemAdmin,
                    allowedOrganizationIds,
                    allowedStoreIds,
                    allowedKioskIds)
                .OrderBy(account => account.UserName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountAsync(
            string? search,
            string? status,
            bool isSystemAdmin,
            IReadOnlySet<Guid> allowedOrganizationIds,
            IReadOnlySet<Guid> allowedStoreIds,
            IReadOnlySet<Guid> allowedKioskIds,
            CancellationToken cancellationToken = default)
        {
            return ApplyFilters(
                    _dbContext.Accounts.AsNoTracking(),
                    search,
                    status,
                    isSystemAdmin,
                    allowedOrganizationIds,
                    allowedStoreIds,
                    allowedKioskIds)
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

        private static IQueryable<Account> ApplyFilters(
            IQueryable<Account> query,
            string? search,
            string? status,
            bool isSystemAdmin,
            IReadOnlySet<Guid> allowedOrganizationIds,
            IReadOnlySet<Guid> allowedStoreIds,
            IReadOnlySet<Guid> allowedKioskIds)
        {
            if (!isSystemAdmin)
            {
                var allowedOrgIds = allowedOrganizationIds.ToArray();
                var allowedStoreScopeIds = allowedStoreIds.ToArray();
                var allowedKioskScopeIds = allowedKioskIds.ToArray();

                if (allowedOrgIds.Length == 0 && allowedStoreScopeIds.Length == 0 && allowedKioskScopeIds.Length == 0)
                {
                    return query.Where(_ => false);
                }

                query = query.Where(account => account.AccountRoles.Any(role =>
                    role.IsActive &&
                    (
                        (role.OrganizationId != null && allowedOrgIds.Contains(role.OrganizationId.Value)) ||
                        (role.StoreId != null && allowedStoreScopeIds.Contains(role.StoreId.Value)) ||
                        (role.KioskId != null && allowedKioskScopeIds.Contains(role.KioskId.Value))
                    )
                ));
            }

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
