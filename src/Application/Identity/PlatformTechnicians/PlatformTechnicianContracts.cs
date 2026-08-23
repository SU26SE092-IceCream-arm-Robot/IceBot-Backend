namespace Application.Identity.PlatformTechnicians;

public sealed record TechnicianScopeRequest(Guid OrganizationId, Guid? StoreId, Guid? KioskId);

public sealed record CreatePlatformTechnicianRequest(
    string UserName,
    string Email,
    string? FullName,
    string? PhoneNumber);

public sealed record UpdatePlatformTechnicianRequest(
    string? FullName,
    string? PhoneNumber,
    long ExpectedAuthorizationVersion);

public sealed record TechnicianLifecycleRequest(string Reason, long ExpectedAuthorizationVersion);

public sealed record ReplaceTechnicianScopesRequest(
    long ExpectedAuthorizationVersion,
    string Reason,
    IReadOnlyList<TechnicianScopeRequest> Scopes);

public sealed record TechnicianResult(
    Guid AccountId,
    string UserName,
    string Email,
    string Status,
    long AuthorizationVersion,
    IReadOnlyList<TechnicianScopeRequest> SupportScopes);
