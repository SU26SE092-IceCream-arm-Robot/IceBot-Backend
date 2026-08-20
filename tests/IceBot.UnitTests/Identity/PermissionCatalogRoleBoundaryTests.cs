using Application.Identity.Roles.Queries;
using Application.Tenants;

namespace IceBot.UnitTests.Identity;

public sealed class PermissionCatalogRoleBoundaryTests
{
    [Fact]
    public void Staff_CanHandleFrontlineActions_ButCannotPerformFinancialOrProductionInterventions()
    {
        var permissions = PermissionCatalog.ResolvePermissionCodes(["Staff"]);

        Assert.Contains("orders.fulfillment.manage", permissions);
        Assert.Contains("orders.refund-flag", permissions);
        Assert.Contains("refunds.view", permissions);
        Assert.Contains("refunds.request", permissions);
        Assert.Contains("alerts.acknowledge", permissions);

        Assert.DoesNotContain("orders.intervention.manage", permissions);
        Assert.DoesNotContain("refunds.process", permissions);
        Assert.DoesNotContain("alerts.resolve", permissions);
    }

    [Fact]
    public void Manager_CanRunOperations_ButCannotPublishProgramsOrRotateEdgeCredentials()
    {
        var permissions = PermissionCatalog.ResolvePermissionCodes(["Manager"]);

        Assert.Contains("orders.intervention.manage", permissions);
        Assert.Contains("refunds.process", permissions);
        Assert.Contains("stores.sales.manage", permissions);

        Assert.DoesNotContain("program.manage", permissions);
        Assert.DoesNotContain("package.install", permissions);
        Assert.DoesNotContain("execution-endpoints.provision", permissions);
        Assert.DoesNotContain("execution-endpoints.credentials.manage", permissions);
    }

    [Fact]
    public void Manager_CanStopOperations_ButCannotChangeKioskOrHardwareLifecycle()
    {
        var permissions = PermissionCatalog.ResolvePermissionCodes(["Manager"]);

        Assert.Contains("kiosks.operations.manage", permissions);
        Assert.Contains("devices.operations.manage", permissions);
        Assert.Contains("execution-endpoints.operations.manage", permissions);
        Assert.Contains("inventory.refill.manage", permissions);
        Assert.Contains("inventory.adjust.manage", permissions);

        Assert.DoesNotContain("kiosks.manage", permissions);
        Assert.DoesNotContain("devices.manage", permissions);
        Assert.DoesNotContain("execution-endpoints.manage", permissions);
        Assert.DoesNotContain("inventory.configure", permissions);
    }

    [Fact]
    public void Staff_CanRunAuditedRefills_ButCannotAdjustOrConfigureInventory()
    {
        var permissions = PermissionCatalog.ResolvePermissionCodes(["Staff"]);

        Assert.Contains("inventory.refill.manage", permissions);
        Assert.DoesNotContain("inventory.adjust.manage", permissions);
        Assert.DoesNotContain("inventory.configure", permissions);
    }

    [Fact]
    public void Technician_CanOperateAndProvisionEdge_ButCannotChangeKioskLifecycleOrViewCommercialDashboards()
    {
        var permissions = PermissionCatalog.ResolvePermissionCodes(["Technician"]);

        Assert.Contains("kiosks.operations.manage", permissions);
        Assert.Contains("execution-endpoints.provision", permissions);
        Assert.Contains("execution-endpoints.credentials.manage", permissions);

        Assert.DoesNotContain("kiosks.manage", permissions);
        Assert.DoesNotContain("kiosks.update", permissions);
        Assert.DoesNotContain("dashboard.view", permissions);
        Assert.DoesNotContain("notifications.view", permissions);
        Assert.DoesNotContain("inventory.refill.manage", permissions);
        Assert.DoesNotContain("inventory.adjust.manage", permissions);
    }

    [Fact]
    public void PaymentDiagnostics_BelongsToTenantFinancialRoles_NotTechnicalOrPlatformDiagnostics()
    {
        var orgAdminPermissions = PermissionCatalog.ResolvePermissionCodes(["OrgAdmin"]);
        var managerPermissions = PermissionCatalog.ResolvePermissionCodes(["Manager"]);
        var technicianPermissions = PermissionCatalog.ResolvePermissionCodes(["Technician"]);
        var systemAdminPermissions = PermissionCatalog.ResolvePermissionCodes(["SystemAdmin"]);

        Assert.Contains("payments.diagnostics.view", orgAdminPermissions);
        Assert.Contains("payments.diagnostics.view", managerPermissions);
        Assert.DoesNotContain("payments.diagnostics.view", technicianPermissions);
        Assert.DoesNotContain("payments.diagnostics.view", systemAdminPermissions);
    }

    [Fact]
    public void ScopeRoleSets_MatchTheNewOperationalAuthorityBoundaries()
    {
        Assert.Contains("Staff", ScopeRoleSets.RefundsRequest);
        Assert.DoesNotContain("Staff", ScopeRoleSets.RefundsProcess);
        Assert.Contains("Technician", ScopeRoleSets.ExecutionEndpointsProvision);
        Assert.DoesNotContain("Manager", ScopeRoleSets.ExecutionEndpointsCredentialsManage);
        Assert.DoesNotContain("Technician", ScopeRoleSets.KiosksManage);
        Assert.Contains("Staff", ScopeRoleSets.InventoryRefillManage);
        Assert.DoesNotContain("Staff", ScopeRoleSets.InventoryAdjustManage);
        Assert.DoesNotContain("Manager", ScopeRoleSets.DevicesManage);
        Assert.Contains("Manager", ScopeRoleSets.DevicesOperationsManage);
        Assert.DoesNotContain("Technician", ScopeRoleSets.PaymentDiagnosticsView);
    }
}
