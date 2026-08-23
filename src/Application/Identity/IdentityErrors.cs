using Application.Shared.Wrappers;

namespace Application.Identity;

public static class IdentityErrors
{
    public static readonly ApiBusinessErrorDefinition OrganizationSuspended = new(
        "IDENTITY.ORGANIZATION_SUSPENDED", 403, "This account belongs only to suspended organizations.");
    public static readonly ApiBusinessErrorDefinition OrganizationInactive = new(
        "IDENTITY.ORGANIZATION_INACTIVE", 403, "This account belongs only to inactive organizations.");
    public static readonly ApiBusinessErrorDefinition OrganizationAccessUnavailable = new(
        "IDENTITY.ORGANIZATION_ACCESS_UNAVAILABLE", 403, "This account has no active organization scope.");

    public static IReadOnlyList<ApiBusinessErrorDefinition> All { get; } =
        [OrganizationSuspended, OrganizationInactive, OrganizationAccessUnavailable];
}
