using Application.Identity.Roles.Queries;
using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Authorization;

public static class AuthorizationPolicyExtensions
{
    public static AuthorizationOptions AddIceBotAuthorizationPolicies(this AuthorizationOptions options)
    {
        foreach (var permission in PermissionCatalog.Matrix)
        {
            options.AddScopedRolePolicy(permission.Policy, permission.Roles.ToArray());
        }

        return options;
    }

    private static void AddScopedRolePolicy(this AuthorizationOptions options, string policyName, params string[] roles)
    {
        options.AddPolicy(policyName, policy =>
            policy.Requirements.Add(new ScopedRoleRequirement(roles)));
    }
}
