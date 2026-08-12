using Application.Identity.Tokens.Claims;
using Domain.ServiceRegistration.Enums;

namespace Application.ServiceRegistration;

public sealed class SubmitServiceRegistrationRequest
{
    public string ContactName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public string BusinessName { get; init; } = null!;
    public string? LegalName { get; init; }
    public string? TaxCode { get; init; }
    public string? Address { get; init; }
    public int? ExpectedLocationCount { get; init; }
    public string? Message { get; init; }
    public Guid PrivacyPolicyRevisionId { get; init; }
    public bool PrivacyPolicyAccepted { get; init; }
}

public sealed class ServiceRegistrationProvisioningRequest
{
    public string OrganizationCode { get; init; } = null!;
    public string OrganizationName { get; init; } = null!;
    public string? OrganizationLegalName { get; init; }
    public string? OrganizationTaxCode { get; init; }
    public string AdminUserName { get; init; } = null!;
    public string AdminEmail { get; init; } = null!;
    public string? AdminFullName { get; init; }
    public bool LocalLoginEnabled { get; init; }
    public bool GoogleLoginEnabled { get; init; }
    public int ExpectedRevision { get; init; }
}

public sealed class ChangeServiceRegistrationStateRequest
{
    public int ExpectedRevision { get; init; }
    public string? Reason { get; init; }
}

public sealed class ServiceRegistrationReceiptResult
{
    public Guid Id { get; init; }
    public string ReferenceCode { get; init; } = null!;
    public ServiceRegistrationStatus Status { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
}

public sealed class ServiceRegistrationResult
{
    public Guid Id { get; init; }
    public string ReferenceCode { get; init; } = null!;
    public string ContactName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public string BusinessName { get; init; } = null!;
    public string? LegalName { get; init; }
    public string? TaxCode { get; init; }
    public string? Address { get; init; }
    public int? ExpectedLocationCount { get; init; }
    public string? Message { get; init; }
    public Guid PrivacyPolicyRevisionId { get; init; }
    public ServiceRegistrationStatus Status { get; init; }
    public string? ReviewReason { get; init; }
    public Guid? ReviewedByAccountId { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
    public Guid? ProvisionedOrganizationId { get; init; }
    public Guid? ProvisionedOrgAdminAccountId { get; init; }
    public Guid? ProvisionedInvitationId { get; init; }
    public string? ProvisioningFailureCode { get; init; }
    public string? ProvisioningFailureMessage { get; init; }
    public int Revision { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class ServiceRegistrationManagementQuery
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public string? Status { get; init; }
    public string? Search { get; init; }
    public DateTimeOffset? CreatedFrom { get; init; }
    public DateTimeOffset? CreatedTo { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public static class ServiceRegistrationPermissionRules
{
    public static bool CanManage(CurrentUserContext user) => user.IsSystemAdmin;
}
