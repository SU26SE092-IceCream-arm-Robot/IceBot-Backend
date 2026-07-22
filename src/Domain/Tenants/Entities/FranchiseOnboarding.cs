using Domain.Common;
using Domain.Tenants.Enums;

namespace Domain.Tenants.Entities;

public sealed class FranchiseOnboarding : BusinessEntity, IOrganizationScoped
{
    public Guid OrganizationId { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public string RequestChecksum { get; private set; } = null!;
    public int RequestSchemaVersion { get; private set; } = 1;
    public string RequestJson { get; private set; } = null!;
    public FranchiseOnboardingStatus Status { get; private set; } = FranchiseOnboardingStatus.Pending;
    public Guid? StoreId { get; private set; }
    public Guid? KioskId { get; private set; }
    public Guid? PackageInstallationId { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }

    Guid? IOrganizationScoped.OrganizationId
    {
        get => OrganizationId;
        set => OrganizationId = value ?? throw new InvalidOperationException(
            "Franchise onboarding organization is required.");
    }

    public static FranchiseOnboarding Start(
        Guid organizationId,
        string idempotencyKey,
        string requestChecksum,
        string requestJson,
        Guid actorId,
        DateTimeOffset now)
    {
        if (organizationId == Guid.Empty || actorId == Guid.Empty ||
            string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(requestChecksum) ||
            string.IsNullOrWhiteSpace(requestJson))
        {
            throw new DomainRuleException("Franchise onboarding scope, identity, payload, and actor are required.");
        }
        if (idempotencyKey.Trim().Length > 200 || requestChecksum.Trim().Length != 64)
            throw new DomainRuleException("Franchise onboarding idempotency key or request checksum is invalid.");

        return new FranchiseOnboarding
        {
            OrganizationId = organizationId,
            IdempotencyKey = idempotencyKey.Trim(),
            RequestChecksum = requestChecksum.Trim(),
            RequestJson = requestJson,
            CreatedAt = now,
            CreatedByAccountId = actorId
        };
    }

    public bool Matches(string checksum) =>
        string.Equals(RequestChecksum, checksum, StringComparison.Ordinal);

    public void MarkRunning(DateTimeOffset now)
    {
        if (Status == FranchiseOnboardingStatus.Cancelled)
            throw new DomainRuleException("Cancelled onboarding cannot be resumed.");
        if (Status == FranchiseOnboardingStatus.ReadyForActivation) return;
        Status = FranchiseOnboardingStatus.Running;
        StartedAt ??= now;
        FailureCode = null;
        FailureMessage = null;
    }

    public void RecordStore(Guid storeId)
    {
        EnsureRunning();
        if (storeId == Guid.Empty) throw new DomainRuleException("Onboarding store is required.");
        StoreId = storeId;
    }

    public void RecordKiosk(Guid kioskId)
    {
        EnsureRunning();
        if (!StoreId.HasValue || kioskId == Guid.Empty)
            throw new DomainRuleException("Onboarding store must exist before kiosk provisioning.");
        KioskId = kioskId;
    }

    public void RecordPackageInstallation(Guid installationId)
    {
        EnsureRunning();
        if (!KioskId.HasValue || installationId == Guid.Empty)
            throw new DomainRuleException("Onboarding kiosk must exist before package installation.");
        PackageInstallationId = installationId;
    }

    public void MarkReady(DateTimeOffset now)
    {
        EnsureRunning();
        if (!StoreId.HasValue || !KioskId.HasValue)
            throw new DomainRuleException("Onboarding requires a store and kiosk before activation review.");
        Status = FranchiseOnboardingStatus.ReadyForActivation;
        ReadyAt = now;
    }

    public void MarkFailed(string code, string message)
    {
        if (Status is FranchiseOnboardingStatus.Cancelled or FranchiseOnboardingStatus.ReadyForActivation)
            throw new DomainRuleException("Terminal onboarding cannot fail.");
        FailureCode = Normalize(code, "Onboarding failure code", 100);
        FailureMessage = Normalize(message, "Onboarding failure message", 1000);
        Status = FranchiseOnboardingStatus.Failed;
    }

    public void Cancel(string reason, DateTimeOffset now)
    {
        if (Status == FranchiseOnboardingStatus.ReadyForActivation)
            throw new DomainRuleException("Ready onboarding cannot be cancelled.");
        if (Status == FranchiseOnboardingStatus.Cancelled) return;
        FailureCode = "CANCELLED";
        FailureMessage = Normalize(reason, "Onboarding cancel reason", 500);
        CancelledAt = now;
        Status = FranchiseOnboardingStatus.Cancelled;
    }

    private void EnsureRunning()
    {
        if (Status != FranchiseOnboardingStatus.Running)
            throw new DomainRuleException("Onboarding checkpoint can only advance while running.");
    }

    private static string Normalize(string value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainRuleException($"{field} is required.");
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new DomainRuleException($"{field} cannot exceed {maxLength} characters.");
        return normalized;
    }
}
