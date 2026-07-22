using Application.Identity.Tokens.Claims;
using Domain.Identity.Entities;

namespace Application.Tenants;

public static class ScopeAccessRules
{
    public static EffectiveScope GetEffectiveScope(
        IReadOnlyCollection<string> allowedRoles,
        CurrentUserContext userContext)
    {
        var matchingScopes = userContext.RoleScopes
            .Where(scope => allowedRoles.Contains(scope.RoleCode, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        return new EffectiveScope(
            matchingScopes
                .Where(scope => scope.OrganizationId.HasValue && !scope.StoreId.HasValue && !scope.KioskId.HasValue)
                .Select(scope => scope.OrganizationId!.Value)
                .ToHashSet(),
            matchingScopes
                .Where(scope => scope.StoreId.HasValue && !scope.KioskId.HasValue)
                .Select(scope => scope.StoreId!.Value)
                .ToHashSet(),
            matchingScopes.Where(scope => scope.KioskId.HasValue).Select(scope => scope.KioskId!.Value).ToHashSet());
    }

    public static bool CanAccessScopedRow(
        IReadOnlyCollection<string> allowedRoles,
        CurrentUserContext userContext,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId)
    {
        if (userContext.IsSystemAdmin && allowedRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return userContext.RoleScopes.Any(scope =>
        {
            if (!allowedRoles.Contains(scope.RoleCode, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (scope.KioskId.HasValue)
            {
                return scope.KioskId == kioskId;
            }

            if (scope.StoreId.HasValue)
            {
                return scope.StoreId == storeId;
            }

            return scope.OrganizationId.HasValue && scope.OrganizationId == organizationId;
        });
    }

    public static bool SharesAnyActiveScope(
        IReadOnlyCollection<string> allowedRoles,
        CurrentUserContext userContext,
        IEnumerable<AccountRole> roles)
    {
        if (userContext.IsSystemAdmin && allowedRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return roles.Any(role => role.IsActive && CanAccessScopedRow(
            allowedRoles,
            userContext,
            role.OrganizationId,
            role.StoreId,
            role.KioskId));
    }
}

public sealed record EffectiveScope(
    IReadOnlySet<Guid> OrganizationIds,
    IReadOnlySet<Guid> StoreIds,
    IReadOnlySet<Guid> KioskIds);

public static class ScopeRoleSets
{
    public static readonly string[] AccountsRead = ["SystemAdmin", "OrgAdmin", "Manager"];
    public static readonly string[] OrganizationsView = ["SystemAdmin", "OrgAdmin"];
    public static readonly string[] OrganizationsUpdate = ["SystemAdmin", "OrgAdmin"];
    public static readonly string[] StoresView = ["SystemAdmin", "OrgAdmin", "Manager"];
    public static readonly string[] StoresManage = ["SystemAdmin", "OrgAdmin"];
    public static readonly string[] StoresUpdate = ["SystemAdmin", "OrgAdmin", "Manager"];
    public static readonly string[] KiosksView = ["SystemAdmin", "OrgAdmin", "Manager", "Technician"];
    public static readonly string[] KiosksManage = ["SystemAdmin", "OrgAdmin", "Manager", "Technician"];
    public static readonly string[] TenantTreeView = ["SystemAdmin", "OrgAdmin", "Manager", "Technician"];
    public static readonly string[] RoleScopeOptionsView = ["SystemAdmin", "OrgAdmin", "Manager"];
    public static readonly string[] ProductsManage = ["SystemAdmin", "Manager"];
    public static readonly string[] ProductTemplatesRead = ["SystemAdmin", "Manager"];
    public static readonly string[] MenusManage = ["SystemAdmin", "Manager"];
    public static readonly string[] InventoryManage = ["SystemAdmin", "Manager", "Staff", "Technician"];
    public static readonly string[] InventoryView = ["SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician"];
    public static readonly string[] InventoryConfigure = ["SystemAdmin", "Manager", "Technician"];
    public static readonly string[] OrdersView = ["SystemAdmin", "OrgAdmin", "Manager", "Staff"];
    public static readonly string[] OrdersManage = ["SystemAdmin", "OrgAdmin", "Manager", "Staff"];
    public static readonly string[] RefundsManage = ["SystemAdmin", "Manager", "Staff"];
    public static readonly string[] PaymentsManage = ["SystemAdmin", "Manager"];
    public static readonly string[] DevicesManage = ["SystemAdmin", "OrgAdmin", "Manager", "Technician"];
    public static readonly string[] DevicesView = ["SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician"];
    public static readonly string[] AlertsView = ["SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician"];
    public static readonly string[] AlertsManage = ["SystemAdmin", "OrgAdmin", "Manager", "Technician"];
    public static readonly string[] MaintenanceView = ["SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician"];
    public static readonly string[] MaintenanceCreate = ["SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician"];
    public static readonly string[] MaintenanceManage = ["SystemAdmin", "OrgAdmin", "Manager", "Technician"];
    public static readonly string[] ArtifactRead = ["SystemAdmin", "OrgAdmin"];
    public static readonly string[] ArtifactUpload = ["SystemAdmin", "OrgAdmin"];
    public static readonly string[] ProgramRead = ["SystemAdmin", "OrgAdmin", "Manager"];
    public static readonly string[] ProgramManage = ["SystemAdmin", "OrgAdmin", "Manager"];
    public static readonly string[] ReleaseRead = ["SystemAdmin", "OrgAdmin", "Manager"];
    public static readonly string[] ReleasePublish = ["SystemAdmin", "OrgAdmin"];
    public static readonly string[] DeploymentRead = ["SystemAdmin", "OrgAdmin", "Manager", "Technician"];
    public static readonly string[] ReleaseDeploy = ["SystemAdmin", "OrgAdmin", "Manager"];
    public static readonly string[] ReleaseRollback = ["SystemAdmin", "OrgAdmin", "Manager"];
    public static readonly string[] PackageRead = ["SystemAdmin", "OrgAdmin", "Manager"];
    public static readonly string[] PackageInstall = ["SystemAdmin", "OrgAdmin", "Manager"];
    public static readonly string[] PackageFork = ["SystemAdmin", "OrgAdmin"];
    public static readonly string[] OperationsView = ["SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician"];
    public static readonly string[] OperationsDiagnostics = ["SystemAdmin", "Technician"];
    public static readonly string[] NotificationsManage = ["SystemAdmin", "OrgAdmin", "Manager", "Technician"];
    public static readonly string[] DashboardView = ["SystemAdmin", "OrgAdmin", "Manager", "Technician"];
}
