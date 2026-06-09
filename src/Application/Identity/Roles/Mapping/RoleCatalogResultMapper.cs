using Application.Identity.Roles.Results;
using Application.Identity.Roles.Rules;
using Domain.Identity.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Application.Identity.Roles.Mapping;

internal static class RoleCatalogResultMapper
{
    public static ManagementRoleResult ToResult(Role role, bool isAssignable)
    {
        var description = role.Description;
        var allowedScopes = new List<string>();
        var requiresScope = true;

        if (RoleCatalogRules.RoleMetadata.TryGetValue(role.Code, out var meta))
        {
            description = string.IsNullOrWhiteSpace(description) ? meta.Description : description;
            allowedScopes.AddRange(meta.AllowedScopes.Select(s => s.ToString()));
            requiresScope = meta.RequiresScope;
        }
        else
        {
            allowedScopes.Add("Global");
        }

        return new ManagementRoleResult
        {
            Code = role.Code,
            Name = role.Name,
            Description = description,
            IsSystemRole = role.IsSystemRole,
            IsAssignable = isAssignable,
            AllowedScopeTypes = allowedScopes,
            RequiresScope = requiresScope
        };
    }
}
