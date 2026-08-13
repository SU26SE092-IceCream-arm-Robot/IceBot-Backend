using Application.Identity.Roles.Queries;
using Application.Tenants;

namespace IceBot.UnitTests.Identity;

public sealed class PlatformOrganizationSalesPermissionTests
{
    [Fact]
    public void SystemAdmin_ReceivesOnlyAggregateOrganizationSalesPermissionForTenantFinancialReporting()
    {
        var permissions = PermissionCatalog.ResolvePermissionCodes(["SystemAdmin"]);

        Assert.Contains("platform.organization-sales.view", permissions);
        Assert.DoesNotContain("orders.view", permissions);
        Assert.DoesNotContain("orders.manage", permissions);
        Assert.DoesNotContain("payments.manage", permissions);
        Assert.DoesNotContain("refunds.manage", permissions);
    }

    [Fact]
    public void ScopedOrderAndPaymentRules_DoNotTreatSystemAdminAsTenantFinancialOperator()
    {
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.OrdersView);
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.OrdersManage);
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.PaymentsManage);
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.RefundsManage);
    }
}
