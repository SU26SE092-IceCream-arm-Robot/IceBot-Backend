using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Authorization;

public static class AuthorizationPolicyExtensions
{
    public static AuthorizationOptions AddIceBotAuthorizationPolicies(this AuthorizationOptions options)
    {
        options.AddScopedRolePolicy("accounts.manage", "SystemAdmin");
        options.AddScopedRolePolicy("accounts.read", "SystemAdmin", "OrgAdmin", "Manager");

        options.AddScopedRolePolicy("payments.manage", "SystemAdmin", "Manager");
        options.AddScopedRolePolicy("products.manage", "SystemAdmin", "Manager");
        options.AddScopedRolePolicy("menus.manage", "SystemAdmin", "Manager");

        options.AddScopedRolePolicy("organizations.manage", "SystemAdmin");
        options.AddScopedRolePolicy("organizations.view", "SystemAdmin", "OrgAdmin");
        options.AddScopedRolePolicy("organizations.update", "SystemAdmin", "OrgAdmin");

        options.AddScopedRolePolicy("stores.view", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("stores.manage", "SystemAdmin", "OrgAdmin");
        options.AddScopedRolePolicy("stores.update", "SystemAdmin", "OrgAdmin", "Manager");

        options.AddScopedRolePolicy("kiosks.view", "SystemAdmin", "OrgAdmin", "Manager", "Technician");
        options.AddScopedRolePolicy("kiosks.manage", "SystemAdmin", "OrgAdmin", "Manager", "Technician");
        options.AddScopedRolePolicy("kiosks.update", "SystemAdmin", "OrgAdmin", "Manager", "Technician");

        options.AddScopedRolePolicy("tenant-tree.view", "SystemAdmin", "OrgAdmin", "Manager", "Technician");
        options.AddScopedRolePolicy("roles.view", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("role-scope-options.view", "SystemAdmin", "OrgAdmin", "Manager");

        options.AddScopedRolePolicy("orders.view", "SystemAdmin", "OrgAdmin", "Manager", "Staff");
        options.AddScopedRolePolicy("orders.manage", "SystemAdmin", "OrgAdmin", "Manager", "Staff");
        options.AddScopedRolePolicy("refunds.manage", "SystemAdmin", "Manager", "Staff");

        options.AddScopedRolePolicy("inventory.view", "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician");
        options.AddScopedRolePolicy("inventory.manage", "SystemAdmin", "Manager", "Staff", "Technician");

        options.AddScopedRolePolicy("maintenance.view", "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician");
        options.AddScopedRolePolicy("maintenance.create", "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician");
        options.AddScopedRolePolicy("maintenance.manage", "SystemAdmin", "OrgAdmin", "Manager", "Technician");

        options.AddScopedRolePolicy("operations.view", "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician");

        options.AddScopedRolePolicy("devices.view", "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician");
        options.AddScopedRolePolicy("devices.manage", "SystemAdmin", "OrgAdmin", "Manager", "Technician");

        options.AddScopedRolePolicy("artifact.upload", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("release.publish", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("release.deploy", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("release.rollback", "SystemAdmin", "OrgAdmin", "Manager");

        return options;
    }

    private static void AddScopedRolePolicy(this AuthorizationOptions options, string policyName, params string[] roles)
    {
        options.AddPolicy(policyName, policy =>
            policy.Requirements.Add(new ScopedRoleRequirement(roles)));
    }
}
