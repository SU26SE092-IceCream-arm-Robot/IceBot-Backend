using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Results;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;

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
        var authorizationError = AccountRoleAssignmentRules.ValidateRoleAssignmentPermission(
            command.UserContext,
            command.UserRoles,
            normalizedRoleCode);
        if (authorizationError is not null)
        {
            return ApiResult<InternalAccountResult>.Fail(authorizationError, 403);
        }

        var scopeError = AccountRoleAssignmentRules.ValidateRequestedScope(command.UserContext, normalizedRoleCode, request);
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
}
