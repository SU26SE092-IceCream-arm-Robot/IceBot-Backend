using Application.Identity.Tokens.Claims;
using Application.Tenants;

namespace Application.Operations.MaintenanceTickets.Rules;

public static class MaintenanceTicketAccessRules
{
    private static readonly string[] AssignmentRoles = ["SystemAdmin", "OrgAdmin", "Manager"];
    private static readonly string[] WorkRoles = ["SystemAdmin", "OrgAdmin", "Manager", "Technician"];
    public static readonly string[] AssigneeRoles = ["Technician", "Manager"];

    public static bool CanView(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId, Guid? assignedToAccountId)
        => ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.MaintenanceView, user, orgId, storeId, kioskId);

    public static bool CanCreate(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
        => ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.MaintenanceCreate, user, orgId, storeId, kioskId);

    public static bool CanAssign(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
        => ScopeAccessRules.CanAccessScopedRow(AssignmentRoles, user, orgId, storeId, kioskId);

    public static bool CanStart(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
        => ScopeAccessRules.CanAccessScopedRow(WorkRoles, user, orgId, storeId, kioskId);

    public static bool CanResolve(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId, Guid? assignedToAccountId)
        => ScopeAccessRules.CanAccessScopedRow(WorkRoles, user, orgId, storeId, kioskId);

    public static bool CanClose(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
        => ScopeAccessRules.CanAccessScopedRow(AssignmentRoles, user, orgId, storeId, kioskId);

    public static bool CanCancel(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
        => ScopeAccessRules.CanAccessScopedRow(AssignmentRoles, user, orgId, storeId, kioskId);

    public static bool CanUpdate(CurrentUserContext user, Guid orgId, Guid storeId, Guid kioskId)
        => ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.MaintenanceManage, user, orgId, storeId, kioskId);
}
