using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Results;
using Application.Identity.Roles.Rules;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Domain.Tenants.Enums;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class AssignInternalAccountRoleCommandHandler
{
    private readonly IIdentityAccountStore _accounts;

    public AssignInternalAccountRoleCommandHandler(IIdentityAccountStore accounts)
    {
        _accounts = accounts;
    }

    public async Task<ApiResult<InternalAccountResult>> HandleAsync(
        AssignInternalAccountRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var accountId = command.AccountId;
        var request = command.Request;
        var assignedByAccountId = command.AssignedByAccountId;

        if (string.IsNullOrWhiteSpace(request.RoleCode))
        {
            return ApiResult<InternalAccountResult>.Fail("Role code is required.", 400);
        }

        var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<InternalAccountResult>.Fail("Account not found.", 404);
        }

        var role = await _accounts.GetRoleByCodeAsync(request.RoleCode.Trim(), cancellationToken);
        if (role is null)
        {
            return ApiResult<InternalAccountResult>.Fail($"Role '{request.RoleCode}' does not exist.", 400);
        }

        var normalizedRoleCode = role.Code.Trim();
        var authorizationError = ValidateRoleAssignmentPermission(
            command.UserContext,
            command.UserRoles,
            normalizedRoleCode);
        if (authorizationError is not null)
        {
            return ApiResult<InternalAccountResult>.Fail(authorizationError, 403);
        }

        var scopeError = ValidateRequestedScope(command.UserContext, normalizedRoleCode, request);
        if (scopeError is not null)
        {
            return ApiResult<InternalAccountResult>.Fail(scopeError, 400);
        }

        var existingRole = account.AccountRoles.FirstOrDefault(accountRole =>
            accountRole.RoleId == role.Id &&
            accountRole.OrganizationId == request.OrganizationId &&
            accountRole.StoreId == request.StoreId &&
            accountRole.KioskId == request.KioskId);

        if (existingRole is not null)
        {
            existingRole.IsActive = true;
            existingRole.AssignedAt = DateTimeOffset.UtcNow;
            existingRole.AssignedByAccountId = assignedByAccountId;
        }
        else
        {
            account.AccountRoles.Add(new AccountRole
            {
                RoleId = role.Id,
                Role = role,
                OrganizationId = request.OrganizationId,
                StoreId = request.StoreId,
                KioskId = request.KioskId,
                AssignedAt = DateTimeOffset.UtcNow,
                AssignedByAccountId = assignedByAccountId
            });
        }

        account.UpdatedAt = DateTimeOffset.UtcNow;
        account.UpdatedByAccountId = assignedByAccountId;
        await _accounts.SaveChangesAsync(cancellationToken);

        return ApiResult<InternalAccountResult>.Success(InternalAccountResultMapper.ToResult(account), "Role assigned.");
    }

    private static string? ValidateRoleAssignmentPermission(
        CurrentUserContext userContext,
        IReadOnlyCollection<string> userRoles,
        string targetRoleCode)
    {
        if (userContext.IsSystemAdmin ||
            userRoles.Contains("SystemAdmin", StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var canAssign = userRoles.Any(roleCode => RoleCatalogRules.CanAssignRole(roleCode, targetRoleCode));
        return canAssign
            ? null
            : "Current account is not allowed to assign this role.";
    }

    private static string? ValidateRequestedScope(
        CurrentUserContext userContext,
        string targetRoleCode,
        Requests.AccountRoleScopeRequest request)
    {
        if (!RoleCatalogRules.RoleMetadata.TryGetValue(targetRoleCode, out var metadata))
        {
            return "Role scope metadata is not configured.";
        }

        var selectedScope = ResolveSelectedScope(request);
        if (!metadata.RequiresScope)
        {
            return selectedScope == TenantScopeType.Global
                ? null
                : "This role does not accept organization, store, or kiosk scope.";
        }

        if (selectedScope == TenantScopeType.Global)
        {
            return "This role requires an organization, store, or kiosk scope.";
        }

        if (!metadata.AllowedScopes.Contains(selectedScope))
        {
            return $"Role '{targetRoleCode}' does not allow {selectedScope} scope.";
        }

        if (userContext.IsSystemAdmin)
        {
            return null;
        }

        return selectedScope switch
        {
            TenantScopeType.Organization => request.OrganizationId.HasValue &&
                                            userContext.AllowedOrganizationIds.Contains(request.OrganizationId.Value)
                ? null
                : "Current account is not allowed to assign this organization scope.",

            TenantScopeType.Store => IsStoreScopeAllowed(userContext, request)
                ? null
                : "Current account is not allowed to assign this store scope.",

            TenantScopeType.Kiosk => IsKioskScopeAllowed(userContext, request)
                ? null
                : "Current account is not allowed to assign this kiosk scope.",

            _ => "Unsupported role scope."
        };
    }

    private static TenantScopeType ResolveSelectedScope(Requests.AccountRoleScopeRequest request)
    {
        if (request.KioskId.HasValue)
        {
            return TenantScopeType.Kiosk;
        }

        if (request.StoreId.HasValue)
        {
            return TenantScopeType.Store;
        }

        return request.OrganizationId.HasValue
            ? TenantScopeType.Organization
            : TenantScopeType.Global;
    }

    private static bool IsStoreScopeAllowed(
        CurrentUserContext userContext,
        Requests.AccountRoleScopeRequest request)
    {
        if (request.StoreId.HasValue && userContext.AllowedStoreIds.Contains(request.StoreId.Value))
        {
            return true;
        }

        return request.OrganizationId.HasValue &&
               userContext.AllowedOrganizationIds.Contains(request.OrganizationId.Value);
    }

    private static bool IsKioskScopeAllowed(
        CurrentUserContext userContext,
        Requests.AccountRoleScopeRequest request)
    {
        if (request.KioskId.HasValue && userContext.AllowedKioskIds.Contains(request.KioskId.Value))
        {
            return true;
        }

        if (request.StoreId.HasValue && userContext.AllowedStoreIds.Contains(request.StoreId.Value))
        {
            return true;
        }

        return request.OrganizationId.HasValue &&
               userContext.AllowedOrganizationIds.Contains(request.OrganizationId.Value);
    }
}
