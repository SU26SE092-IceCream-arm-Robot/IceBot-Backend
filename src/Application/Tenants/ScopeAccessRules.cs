using Application.Identity.Tokens.Claims;
using Application.Identity.Roles.Queries;
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

    public static IReadOnlyList<AuthorizationScopeSnapshot> GetAuthorizingScopeSnapshots(
        IReadOnlyCollection<string> allowedRoles,
        CurrentUserContext userContext,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId)
    {
        if (userContext.IsSystemAdmin && allowedRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
        {
            return [new AuthorizationScopeSnapshot("SystemAdmin", null, null, null)];
        }

        return userContext.RoleScopes
            .Where(scope => allowedRoles.Contains(scope.RoleCode, StringComparer.OrdinalIgnoreCase))
            .Where(scope =>
                scope.KioskId.HasValue
                    ? scope.KioskId == kioskId
                    : scope.StoreId.HasValue
                        ? scope.StoreId == storeId
                        : scope.OrganizationId.HasValue && scope.OrganizationId == organizationId)
            .Select(scope => new AuthorizationScopeSnapshot(
                scope.RoleCode,
                scope.OrganizationId,
                scope.StoreId,
                scope.KioskId))
            .OrderByDescending(scope => scope.KioskId.HasValue)
            .ThenByDescending(scope => scope.StoreId.HasValue)
            .ThenBy(scope => scope.RoleCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

public sealed record AuthorizationScopeSnapshot(
    string RoleCode,
    Guid? OrganizationId,
    Guid? StoreId,
    Guid? KioskId);

public static class ScopeRoleSets
{
    public static readonly IReadOnlyList<string> AccountsRead = Roles("accounts.read");
    public static readonly IReadOnlyList<string> OrganizationsView = Roles("organizations.view");
    public static readonly IReadOnlyList<string> OrganizationsUpdate = Roles("organizations.update");
    public static readonly IReadOnlyList<string> StoresView = Roles("stores.view");
    public static readonly IReadOnlyList<string> StoresManage = Roles("stores.manage");
    public static readonly IReadOnlyList<string> StoresUpdate = Roles("stores.update");
    public static readonly IReadOnlyList<string> StoresSalesManage = Roles("stores.sales.manage");
    public static readonly IReadOnlyList<string> KiosksView = Roles("kiosks.view");
    public static readonly IReadOnlyList<string> KiosksManage = Roles("kiosks.manage");
    public static readonly IReadOnlyList<string> KiosksUpdate = Roles("kiosks.update");
    public static readonly IReadOnlyList<string> KiosksOperationsManage = Roles("kiosks.operations.manage");
    public static readonly IReadOnlyList<string> TenantTreeView = Roles("tenant-tree.view");
    public static readonly IReadOnlyList<string> AccountsManage = Roles("accounts.manage");
    public static readonly IReadOnlyList<string> ProductsManage = Roles("products.manage");
    public static readonly IReadOnlyList<string> ProductTemplatesRead = Roles("product-templates.read");
    public static readonly IReadOnlyList<string> MenusManage = Roles("menus.manage");
    public static readonly IReadOnlyList<string> MenuItemAvailabilityManage = Roles("menu-items.availability.manage");
    public static readonly IReadOnlyList<string> InventoryRefillManage = Roles("inventory.refill.manage");
    public static readonly IReadOnlyList<string> InventoryAdjustManage = Roles("inventory.adjust.manage");
    public static readonly IReadOnlyList<string> InventoryView = Roles("inventory.view");
    public static readonly IReadOnlyList<string> InventoryConfigure = Roles("inventory.configure");
    public static readonly IReadOnlyList<string> OrdersView = Roles("orders.view");
    public static readonly IReadOnlyList<string> OrdersFulfillmentManage = Roles("orders.fulfillment.manage");
    public static readonly IReadOnlyList<string> OrdersInterventionManage = Roles("orders.intervention.manage");
    public static readonly IReadOnlyList<string> OrdersRefundFlag = Roles("orders.refund-flag");
    public static readonly IReadOnlyList<string> RefundsView = Roles("refunds.view");
    public static readonly IReadOnlyList<string> RefundsRequest = Roles("refunds.request");
    public static readonly IReadOnlyList<string> RefundsProcess = Roles("refunds.process");
    public static readonly IReadOnlyList<string> PaymentsManage = Roles("payments.manage");
    public static readonly IReadOnlyList<string> PaymentDiagnosticsView = Roles("payments.diagnostics.view");
    public static readonly IReadOnlyList<string> PaymentReconciliationView = Roles("payments.reconciliation.view");
    public static readonly IReadOnlyList<string> CashPaymentsConfirm = Roles("cash-payments.confirm");
    public static readonly IReadOnlyList<string> DevicesManage = Roles("devices.manage");
    public static readonly IReadOnlyList<string> DevicesOperationsManage = Roles("devices.operations.manage");
    public static readonly IReadOnlyList<string> DevicesView = Roles("devices.view");
    public static readonly IReadOnlyList<string> ExecutionEndpointsManage = Roles("execution-endpoints.manage");
    public static readonly IReadOnlyList<string> ExecutionEndpointsOperationsManage = Roles("execution-endpoints.operations.manage");
    public static readonly IReadOnlyList<string> ExecutionEndpointsProvision = Roles("execution-endpoints.provision");
    public static readonly IReadOnlyList<string> ExecutionEndpointsCredentialsManage = Roles("execution-endpoints.credentials.manage");
    public static readonly IReadOnlyList<string> AlertsView = Roles("alerts.view");
    public static readonly IReadOnlyList<string> AlertsAcknowledge = Roles("alerts.acknowledge");
    public static readonly IReadOnlyList<string> AlertsResolve = Roles("alerts.resolve");
    public static readonly IReadOnlyList<string> MaintenanceView = Roles("maintenance.view");
    public static readonly IReadOnlyList<string> MaintenanceCreate = Roles("maintenance.create");
    public static readonly IReadOnlyList<string> MaintenanceManage = Roles("maintenance.manage");
    public static readonly IReadOnlyList<string> ArtifactRead = Roles("artifact.read");
    public static readonly IReadOnlyList<string> ArtifactUpload = Roles("artifact.upload");
    public static readonly IReadOnlyList<string> ProgramRead = Roles("program.read");
    public static readonly IReadOnlyList<string> ProgramManage = Roles("program.manage");
    public static readonly IReadOnlyList<string> ReleaseRead = Roles("release.read");
    public static readonly IReadOnlyList<string> ReleasePublish = Roles("release.publish");
    public static readonly IReadOnlyList<string> DeploymentRead = Roles("deployment.read");
    public static readonly IReadOnlyList<string> ReleaseDeploy = Roles("release.deploy");
    public static readonly IReadOnlyList<string> ReleaseRollback = Roles("release.rollback");
    public static readonly IReadOnlyList<string> PackageRead = Roles("package.read");
    public static readonly IReadOnlyList<string> PackageInstall = Roles("package.install");
    public static readonly IReadOnlyList<string> PackageFork = Roles("package.fork");
    public static readonly IReadOnlyList<string> OperationsView = Roles("operations.view");
    public static readonly IReadOnlyList<string> OperationsDiagnostics = Roles("operations.diagnostics");
    public static readonly IReadOnlyList<string> NotificationsView = Roles("notifications.view");
    public static readonly IReadOnlyList<string> NotificationsManage = Roles("notifications.manage");
    public static readonly IReadOnlyList<string> DashboardView = Roles("dashboard.view");

    private static IReadOnlyList<string> Roles(string policy) => PermissionCatalog.GetRoles(policy);
}
