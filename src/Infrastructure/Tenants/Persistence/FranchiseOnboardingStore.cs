using Application.Tenants.Onboarding;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Tenants.Persistence;

public sealed class FranchiseOnboardingStore(IceBotDbContext db) : IFranchiseOnboardingStore
{
    public Task<FranchiseOnboarding?> FindByIdempotencyKeyAsync(
        Guid organizationId,
        string key,
        CancellationToken cancellationToken = default) =>
        db.FranchiseOnboardings.AsNoTracking().FirstOrDefaultAsync(
            x => x.OrganizationId == organizationId && x.IdempotencyKey == key && x.DeletedAt == null,
            cancellationToken);

    public Task<FranchiseOnboarding?> GetAsync(
        Guid organizationId,
        Guid onboardingId,
        bool tracked,
        CancellationToken cancellationToken = default)
    {
        IQueryable<FranchiseOnboarding> query = db.FranchiseOnboardings;
        if (!tracked) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(
            x => x.OrganizationId == organizationId && x.Id == onboardingId && x.DeletedAt == null,
            cancellationToken);
    }

    public Task<Guid?> FindStoreIdByCodeAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default) =>
        db.Stores.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.Code == code && x.DeletedAt == null)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Guid?> FindKioskIdByCodeAsync(
        Guid organizationId,
        Guid storeId,
        string code,
        CancellationToken cancellationToken = default) =>
        db.Kiosks.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.StoreId == storeId && x.Code == code &&
                x.DeletedAt == null)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> TryInsertAsync(
        FranchiseOnboarding onboarding,
        CancellationToken cancellationToken = default)
    {
        var entry = db.FranchiseOnboardings.Add(onboarding);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            entry.State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> TryClaimAsync(
        Guid organizationId,
        Guid onboardingId,
        DateTimeOffset now,
        DateTimeOffset staleBefore,
        CancellationToken cancellationToken = default)
    {
        var affected = await db.FranchiseOnboardings
            .Where(x => x.OrganizationId == organizationId && x.Id == onboardingId && x.DeletedAt == null &&
                (x.Status == FranchiseOnboardingStatus.Pending ||
                 x.Status == FranchiseOnboardingStatus.Failed ||
                 (x.Status == FranchiseOnboardingStatus.Running &&
                  (x.UpdatedAt == null || x.UpdatedAt < staleBefore))))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, FranchiseOnboardingStatus.Running)
                .SetProperty(x => x.StartedAt, x => x.StartedAt ?? now)
                .SetProperty(x => x.UpdatedAt, now)
                .SetProperty(x => x.FailureCode, (string?)null)
                .SetProperty(x => x.FailureMessage, (string?)null), cancellationToken);
        db.ChangeTracker.Clear();
        return affected == 1;
    }

    public async Task<bool> TryCancelAsync(
        Guid organizationId,
        Guid onboardingId,
        string reason,
        Guid actorId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var affected = await db.FranchiseOnboardings
            .Where(x => x.OrganizationId == organizationId && x.Id == onboardingId && x.DeletedAt == null &&
                (x.Status == FranchiseOnboardingStatus.Pending || x.Status == FranchiseOnboardingStatus.Failed))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, FranchiseOnboardingStatus.Cancelled)
                .SetProperty(x => x.FailureCode, "CANCELLED")
                .SetProperty(x => x.FailureMessage, reason)
                .SetProperty(x => x.CancelledAt, now)
                .SetProperty(x => x.UpdatedAt, now)
                .SetProperty(x => x.UpdatedByAccountId, actorId), cancellationToken);
        db.ChangeTracker.Clear();
        return affected == 1;
    }

    public Task<int> CountAsync(
        Guid organizationId,
        FranchiseOnboardingStatus? status,
        CancellationToken cancellationToken = default) =>
        Filter(organizationId, status).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<FranchiseOnboarding>> ListAsync(
        Guid organizationId,
        FranchiseOnboardingStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        await Filter(organizationId, status)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    private IQueryable<FranchiseOnboarding> Filter(
        Guid organizationId,
        FranchiseOnboardingStatus? status)
    {
        var query = db.FranchiseOnboardings.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.DeletedAt == null);
        return status.HasValue ? query.Where(x => x.Status == status.Value) : query;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
