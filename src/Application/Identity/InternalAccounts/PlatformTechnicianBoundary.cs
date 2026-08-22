using Domain.Identity.Entities;

namespace Application.Identity.InternalAccounts;

internal static class PlatformTechnicianBoundary
{
    public const string RoleCode = "Technician";

    public static bool IsTechnicianRole(string? roleCode) =>
        string.Equals(roleCode?.Trim(), RoleCode, StringComparison.OrdinalIgnoreCase);

    public static bool HasMixedActiveRoles(Account account) =>
        account.PlatformTechnicianProfile is not null && account.AccountRoles.Any(role =>
            role.IsActive);
}
