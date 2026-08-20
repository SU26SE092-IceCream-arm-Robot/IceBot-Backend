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
        Assert.DoesNotContain("orders.fulfillment.manage", permissions);
        Assert.DoesNotContain("orders.intervention.manage", permissions);
        Assert.DoesNotContain("orders.refund-flag", permissions);
        Assert.DoesNotContain("payments.manage", permissions);
        Assert.DoesNotContain("refunds.view", permissions);
        Assert.DoesNotContain("refunds.request", permissions);
        Assert.DoesNotContain("refunds.process", permissions);
    }

    [Fact]
    public void ScopedOrderAndPaymentRules_DoNotTreatSystemAdminAsTenantFinancialOperator()
    {
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.OrdersView);
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.OrdersFulfillmentManage);
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.OrdersInterventionManage);
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.OrdersRefundFlag);
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.PaymentsManage);
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.RefundsView);
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.RefundsRequest);
        Assert.DoesNotContain("SystemAdmin", ScopeRoleSets.RefundsProcess);
    }
}
