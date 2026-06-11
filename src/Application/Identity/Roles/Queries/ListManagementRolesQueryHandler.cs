using Application.Identity.Abstractions;
using Application.Identity.Roles.Mapping;
using Application.Identity.Roles.Results;
using Application.Identity.Roles.Rules;
using Application.Shared.Wrappers;

namespace Application.Identity.Roles.Queries;

public sealed class ListManagementRolesQueryHandler
{
    private readonly IIdentityAccountStore _accountStore;

    public ListManagementRolesQueryHandler(IIdentityAccountStore accountStore)
    {
        _accountStore = accountStore;
    }

    public async Task<ApiResult<IEnumerable<ManagementRoleResult>>> HandleAsync(
        ListManagementRolesQuery query,
        CancellationToken cancellationToken = default)
    {
        var dbRoles = await _accountStore.ListActiveRolesAsync(cancellationToken);
        var callerRoles = query.UserRoles ?? Array.Empty<string>();

        var results = new List<ManagementRoleResult>();
        var isSystemAdmin = query.UserContext.IsSystemAdmin || callerRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase);

        foreach (var role in dbRoles)
        {
            if (isSystemAdmin)
            {
                results.Add(RoleCatalogResultMapper.ToResult(role, isAssignable: true));
            }
            else
            {
                var canAssign = callerRoles.Any(callerRole => RoleCatalogRules.CanAssignRole(callerRole, role.Code));
                if (canAssign)
                {
                    results.Add(RoleCatalogResultMapper.ToResult(role, isAssignable: true));
                }
            }
        }

        return ApiResult<IEnumerable<ManagementRoleResult>>.Success(results, "Roles catalog retrieved successfully.");
    }
}
