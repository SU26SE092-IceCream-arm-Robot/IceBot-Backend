using Application.Identity.Tokens.Claims;
using Application.Tenants.Kiosks.Requests;
using Application.Tenants.Stores.Requests;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace Application.Tenants.Onboarding;

public sealed class StartFranchiseOnboardingRequest
{
    public required CreateStoreRequest Store { get; init; }
    public required CreateKioskRequest Kiosk { get; init; }
    public Guid? ProductionPackageId { get; init; }
    public Guid? ProductionPackageVersionId { get; init; }
    public IReadOnlyCollection<string> ProductSourceKeys { get; init; } = [];
}

public sealed record FranchiseOnboardingResult(
    Guid Id,
    Guid OrganizationId,
    string Status,
    Guid? StoreId,
    Guid? KioskId,
    Guid? PackageInstallationId,
    string? FailureCode,
    string? FailureMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadyAt)
{
    public static FranchiseOnboardingResult From(FranchiseOnboarding onboarding) => new(
        onboarding.Id,
        onboarding.OrganizationId,
        onboarding.Status.ToString(),
        onboarding.StoreId,
        onboarding.KioskId,
        onboarding.PackageInstallationId,
        onboarding.FailureCode,
        onboarding.FailureMessage,
        onboarding.CreatedAt,
        onboarding.ReadyAt);
}

public sealed class StartFranchiseOnboardingCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required StartFranchiseOnboardingRequest Request { get; init; }
}

public interface IFranchiseOnboardingStore
{
    Task<FranchiseOnboarding?> FindByIdempotencyKeyAsync(Guid organizationId, string key, CancellationToken cancellationToken = default);
    Task<FranchiseOnboarding?> GetAsync(Guid organizationId, Guid onboardingId, bool tracked, CancellationToken cancellationToken = default);
    Task<Guid?> FindStoreIdByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken = default);
    Task<Guid?> FindKioskIdByCodeAsync(Guid organizationId, Guid storeId, string code, CancellationToken cancellationToken = default);
    Task<bool> TryInsertAsync(FranchiseOnboarding onboarding, CancellationToken cancellationToken = default);
    Task<bool> TryClaimAsync(Guid organizationId, Guid onboardingId, DateTimeOffset now, DateTimeOffset staleBefore, CancellationToken cancellationToken = default);
    Task<bool> TryCancelAsync(Guid organizationId, Guid onboardingId, string reason, Guid actorId,
        DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Guid organizationId, FranchiseOnboardingStatus? status,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FranchiseOnboarding>> ListAsync(Guid organizationId, FranchiseOnboardingStatus? status,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
