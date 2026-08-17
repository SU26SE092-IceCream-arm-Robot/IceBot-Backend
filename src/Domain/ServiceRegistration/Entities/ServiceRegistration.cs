using Domain.Common;
using Domain.ServiceRegistration.Enums;

namespace Domain.ServiceRegistration.Entities;

public sealed class ServiceRegistration : BusinessEntity
{
    public string ReferenceCode { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;
    public string RequestChecksum { get; private set; } = null!;
    public string ContactName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public string? NormalizedPhoneNumber { get; private set; }
    public string BusinessName { get; private set; } = null!;
    public string? LegalName { get; private set; }
    public string? TaxCode { get; private set; }
    public string? Address { get; private set; }
    public int? ExpectedLocationCount { get; private set; }
    public string? Message { get; private set; }
    public Guid PrivacyPolicyRevisionId { get; private set; }
    public DateTimeOffset PrivacyPolicyAcceptedAt { get; private set; }
    public ServiceRegistrationStatus Status { get; private set; } = ServiceRegistrationStatus.Submitted;
    public string? ReviewReason { get; private set; }
    public Guid? ReviewedByAccountId { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ApprovedProvisioningJson { get; private set; }
    public Guid? ProvisionedOrganizationId { get; private set; }
    public Guid? ProvisionedOrgAdminAccountId { get; private set; }
    public Guid? ProvisionedInvitationId { get; private set; }
    public string? ProvisioningFailureCode { get; private set; }
    public string? ProvisioningFailureMessage { get; private set; }
    public int Revision { get; private set; } = 1;

    private ServiceRegistration() { }

    public static ServiceRegistration Submit(
        string referenceCode, string idempotencyKey, string requestChecksum,
        string contactName, string email, string normalizedEmail,
        string? phoneNumber, string? normalizedPhoneNumber, string businessName,
        string? legalName, string? taxCode, string? address, int? expectedLocationCount,
        string? message, Guid privacyPolicyRevisionId, DateTimeOffset now)
    {
        if (privacyPolicyRevisionId == Guid.Empty) throw new DomainRuleException("A published privacy policy revision is required.");
        return new ServiceRegistration
        {
            ReferenceCode = Required(referenceCode, nameof(referenceCode), 40),
            IdempotencyKey = Required(idempotencyKey, nameof(idempotencyKey), 200),
            RequestChecksum = Required(requestChecksum, nameof(requestChecksum), 64),
            ContactName = Required(contactName, nameof(contactName), 200),
            Email = Required(email, nameof(email), 320),
            NormalizedEmail = Required(normalizedEmail, nameof(normalizedEmail), 320),
            PhoneNumber = Optional(phoneNumber, 50),
            NormalizedPhoneNumber = Optional(normalizedPhoneNumber, 50),
            BusinessName = Required(businessName, nameof(businessName), 200),
            LegalName = Optional(legalName, 300),
            TaxCode = Optional(taxCode, 100),
            Address = Optional(address, 500),
            ExpectedLocationCount = expectedLocationCount,
            Message = Optional(message, 2_000),
            PrivacyPolicyRevisionId = privacyPolicyRevisionId,
            PrivacyPolicyAcceptedAt = now,
            CreatedAt = now
        };
    }

    public bool Matches(string checksum) => string.Equals(RequestChecksum, checksum, StringComparison.Ordinal);

    public void StartReview(Guid actorId, int expectedRevision, DateTimeOffset now)
    {
        EnsureRevision(expectedRevision);
        if (Status != ServiceRegistrationStatus.Submitted) throw new DomainRuleException("Only submitted registrations can enter review.");
        Status = ServiceRegistrationStatus.UnderReview;
        ReviewedByAccountId = RequiredActor(actorId);
        ReviewedAt = now;
        Advance(actorId, now);
    }

    public void Reject(Guid actorId, string reason, int expectedRevision, DateTimeOffset now)
    {
        EnsureRevision(expectedRevision);
        if (Status is not (ServiceRegistrationStatus.Submitted or ServiceRegistrationStatus.UnderReview))
            throw new DomainRuleException("Only submitted or in-review registrations can be rejected.");
        Status = ServiceRegistrationStatus.Rejected;
        ReviewReason = Required(reason, nameof(reason), 1_000);
        ReviewedByAccountId = RequiredActor(actorId);
        ReviewedAt = now;
        Advance(actorId, now);
    }

    public void BeginProvisioning(Guid actorId, string provisioningJson, int expectedRevision, DateTimeOffset now, bool retry = false)
    {
        EnsureRevision(expectedRevision);
        var allowed = retry ? Status == ServiceRegistrationStatus.ProvisioningFailed : Status is ServiceRegistrationStatus.Submitted or ServiceRegistrationStatus.UnderReview;
        if (!allowed) throw new DomainRuleException(retry ? "Only failed provisioning can be retried." : "Registration cannot be approved in its current state.");
        Status = ServiceRegistrationStatus.Provisioning;
        ApprovedProvisioningJson = Required(provisioningJson, nameof(provisioningJson), 8_000);
        ReviewReason = null;
        ReviewedByAccountId = RequiredActor(actorId);
        ReviewedAt = now;
        ProvisioningFailureCode = null;
        ProvisioningFailureMessage = null;
        Advance(actorId, now);
    }

    public void RecordProvisioningFailure(string code, string message, DateTimeOffset now)
    {
        if (Status != ServiceRegistrationStatus.Provisioning) throw new DomainRuleException("Only active provisioning can fail.");
        Status = ServiceRegistrationStatus.ProvisioningFailed;
        ProvisioningFailureCode = Required(code, nameof(code), 100);
        ProvisioningFailureMessage = Required(message, nameof(message), 1_000);
        UpdatedAt = now;
        Revision++;
    }

    public void CompleteProvisioning(Guid organizationId, Guid accountId, Guid? invitationId, DateTimeOffset now)
    {
        if (Status != ServiceRegistrationStatus.Provisioning) throw new DomainRuleException("Only active provisioning can complete.");
        if (organizationId == Guid.Empty || accountId == Guid.Empty || invitationId == Guid.Empty) throw new DomainRuleException("Provisioning references are invalid.");
        Status = ServiceRegistrationStatus.Provisioned;
        ProvisionedOrganizationId = organizationId;
        ProvisionedOrgAdminAccountId = accountId;
        ProvisionedInvitationId = invitationId;
        ProvisioningFailureCode = null;
        ProvisioningFailureMessage = null;
        UpdatedAt = now;
        Revision++;
    }

    private void EnsureRevision(int expectedRevision)
    {
        if (expectedRevision != Revision) throw new DomainRuleException("The service registration was changed by another user. Refresh and try again.");
    }

    private void Advance(Guid actorId, DateTimeOffset now)
    {
        UpdatedByAccountId = actorId;
        UpdatedAt = now;
        Revision++;
    }

    private static Guid RequiredActor(Guid actorId) => actorId == Guid.Empty ? throw new DomainRuleException("Actor is required.") : actorId;
    private static string Required(string value, string field, int maxLength)
    {
        var result = Optional(value, maxLength);
        return string.IsNullOrWhiteSpace(result) ? throw new DomainRuleException($"{field} is required.") : result;
    }
    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = value.Trim();
        if (result.Length > maxLength) throw new DomainRuleException($"Value cannot exceed {maxLength} characters.");
        return result;
    }
}
