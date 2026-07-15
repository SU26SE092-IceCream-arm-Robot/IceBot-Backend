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
        options.AddScopedRolePolicy("product-categories.read", "SystemAdmin", "Manager");
        options.AddScopedRolePolicy("product-categories.manage", "SystemAdmin");
        options.AddScopedRolePolicy("ingredients.read", "SystemAdmin", "Manager");
        options.AddScopedRolePolicy("ingredients.manage", "SystemAdmin");
        options.AddScopedRolePolicy("product-templates.read", "SystemAdmin", "Manager");
        options.AddScopedRolePolicy("product-templates.manage", "SystemAdmin");
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
        options.AddScopedRolePolicy("inventory.configure", "SystemAdmin", "Manager", "Technician");

        options.AddScopedRolePolicy("maintenance.view", "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician");
        options.AddScopedRolePolicy("maintenance.create", "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician");
        options.AddScopedRolePolicy("maintenance.manage", "SystemAdmin", "OrgAdmin", "Manager", "Technician");
        options.AddScopedRolePolicy("sync-dead-letters.manage", "SystemAdmin");

        options.AddScopedRolePolicy("alerts.view", "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician");
        options.AddScopedRolePolicy("alerts.manage", "SystemAdmin", "OrgAdmin", "Manager", "Technician");

        options.AddScopedRolePolicy("operations.view", "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician");
        options.AddScopedRolePolicy("operations.diagnostics", "SystemAdmin", "Technician");

        options.AddScopedRolePolicy("devices.view", "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician");
        options.AddScopedRolePolicy("devices.manage", "SystemAdmin", "OrgAdmin", "Manager", "Technician");
        options.AddScopedRolePolicy("device-catalog.read", "SystemAdmin", "OrgAdmin", "Manager", "Staff", "Technician");
        options.AddScopedRolePolicy("device-catalog.manage", "SystemAdmin");

        options.AddScopedRolePolicy("artifact.read", "SystemAdmin", "OrgAdmin");
        options.AddScopedRolePolicy("artifact.upload", "SystemAdmin", "OrgAdmin");
        options.AddScopedRolePolicy("artifact-template.read", "SystemAdmin", "OrgAdmin");
        options.AddScopedRolePolicy("artifact-template.manage", "SystemAdmin");
        options.AddScopedRolePolicy("program.read", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("program.manage", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("release.read", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("release.publish", "SystemAdmin", "OrgAdmin");
        options.AddScopedRolePolicy("deployment.read", "SystemAdmin", "OrgAdmin", "Manager", "Technician");
        options.AddScopedRolePolicy("release.deploy", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("release.rollback", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("package.read", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("package.manage", "SystemAdmin");
        options.AddScopedRolePolicy("package.install", "SystemAdmin", "OrgAdmin", "Manager");
        options.AddScopedRolePolicy("package.fork", "SystemAdmin", "OrgAdmin");

        return options;
    }

    private static void AddScopedRolePolicy(this AuthorizationOptions options, string policyName, params string[] roles)
    {
        options.AddPolicy(policyName, policy =>
            policy.Requirements.Add(new ScopedRoleRequirement(roles)));
    }
}
