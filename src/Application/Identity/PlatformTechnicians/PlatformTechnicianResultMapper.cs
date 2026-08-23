using Domain.Identity.Entities;

namespace Application.Identity.PlatformTechnicians;

internal static class PlatformTechnicianResultMapper
{
    public static TechnicianResult ToResult(Account account) => new(
        account.Id,
        account.UserName,
        account.Email,
        account.Status.ToString(),
        account.AuthorizationVersion,
        account.TechnicianSupportGrants
            .Where(grant => grant.IsActive)
            .Select(grant => new TechnicianScopeRequest(
                grant.OrganizationId,
                grant.StoreId,
                grant.KioskId))
            .ToArray());
}
