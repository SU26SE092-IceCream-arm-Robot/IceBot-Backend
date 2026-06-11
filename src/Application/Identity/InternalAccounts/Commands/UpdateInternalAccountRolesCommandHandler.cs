using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Results;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class UpdateInternalAccountRolesCommandHandler
{
    private readonly IIdentityAccountStore _accounts;

    public UpdateInternalAccountRolesCommandHandler(IIdentityAccountStore accounts)
    {
        _accounts = accounts;
    }

    public async Task<ApiResult<InternalAccountResult>> HandleAsync(
        UpdateInternalAccountRolesCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Request.Roles.Count == 0)
        {
            return ApiResult<InternalAccountResult>.Fail("At least one role assignment is required.", 400);
        }

        var duplicate = command.Request.Roles
            .GroupBy(role => new
            {
                RoleCode = role.RoleCode?.Trim().ToUpperInvariant(),
                role.OrganizationId,
                role.StoreId,
                role.KioskId
            })
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            return ApiResult<InternalAccountResult>.Fail("Duplicate role assignment in request.", 400);
        }

        var account = await _accounts.GetByIdAsync(command.AccountId, asNoTracking: false, cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<InternalAccountResult>.Fail("Account not found.", 404);
        }

        var requestedRoles = new List<(Role Role, Requests.AccountRoleScopeRequest Request)>();
        foreach (var requestRole in command.Request.Roles)
        {
            if (string.IsNullOrWhiteSpace(requestRole.RoleCode))
            {
                return ApiResult<InternalAccountResult>.Fail("Role code is required.", 400);
            }

            var role = await _accounts.GetRoleByCodeAsync(requestRole.RoleCode.Trim(), cancellationToken);
            if (role is null)
            {
                return ApiResult<InternalAccountResult>.Fail($"Role '{requestRole.RoleCode}' does not exist.", 400);
            }

            var authorizationError = AccountRoleAssignmentRules.ValidateRoleAssignmentPermission(
                command.UserContext,
                command.UserRoles,
                role.Code);
            if (authorizationError is not null)
            {
                return ApiResult<InternalAccountResult>.Fail(authorizationError, 403);
            }

            var scopeError = AccountRoleAssignmentRules.ValidateRequestedScope(command.UserContext, role.Code, requestRole);
            if (scopeError is not null)
            {
                return ApiResult<InternalAccountResult>.Fail(scopeError, 400);
            }

            requestedRoles.Add((role, requestRole));
        }

        foreach (var currentRole in account.AccountRoles)
        {
            currentRole.IsActive = false;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var (role, requestRole) in requestedRoles)
        {
            var existingRole = account.AccountRoles.FirstOrDefault(accountRole =>
                accountRole.RoleId == role.Id &&
                accountRole.OrganizationId == requestRole.OrganizationId &&
                accountRole.StoreId == requestRole.StoreId &&
                accountRole.KioskId == requestRole.KioskId);

            if (existingRole is not null)
            {
                existingRole.IsActive = true;
                existingRole.AssignedAt = now;
                existingRole.AssignedByAccountId = command.UpdatedByAccountId;
            }
            else
            {
                account.AccountRoles.Add(new AccountRole
                {
                    RoleId = role.Id,
                    Role = role,
                    OrganizationId = requestRole.OrganizationId,
                    StoreId = requestRole.StoreId,
                    KioskId = requestRole.KioskId,
                    AssignedAt = now,
                    AssignedByAccountId = command.UpdatedByAccountId
                });
            }
        }

        account.UpdatedAt = now;
        account.UpdatedByAccountId = command.UpdatedByAccountId;
        await _accounts.SaveChangesAsync(cancellationToken);

        return ApiResult<InternalAccountResult>.Success(
            InternalAccountResultMapper.ToResult(account),
            "Account roles updated.");
    }
}
