using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Requests;
using Application.Identity.InternalAccounts.Results;
using Application.Identity.Provisioning;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class CreateInternalAccountCommandHandler
{
    private readonly IIdentityAccountStore _accounts;
    private readonly TenantAccountCredentialService _credentials;

    /* TARGET FLOW - temporarily disabled for the demo.
     * Restore AccountInvitationService as a constructor dependency, create the account with
     * Status = Invited, and call CreateInvitationAsync after account persistence. The complete
     * invitation implementation remains in AccountInvitationService and AcceptInvitationCommandHandler.
     */

    public CreateInternalAccountCommandHandler(
        IIdentityAccountStore accounts,
        TenantAccountCredentialService credentials)
    {
        _accounts = accounts;
        _credentials = credentials;
    }

    public async Task<ApiResult<InternalAccountResult>> HandleAsync(
        CreateInternalAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var createdByAccountId = command.CreatedByAccountId;

        if (command.OrganizationId == Guid.Empty)
        {
            return ApiResult<InternalAccountResult>.Fail("Organization id is required.", 400);
        }

        var validationError = InternalAccountRequestValidator.ValidateRequest(request);
        if (validationError is not null)
        {
            return ApiResult<InternalAccountResult>.Fail(validationError);
        }

        var email = InternalAccountNormalizer.NormalizeEmail(request.Email);
        var userName = InternalAccountNormalizer.NormalizeUserName(request.UserName);

        if (await _accounts.ExistsByEmailOrUserNameAsync(email, userName, cancellationToken))
        {
            return ApiResult<InternalAccountResult>.Fail("Account already exists.", 409);
        }

        var googleEmail = request.GoogleLoginEnabled
            ? InternalAccountNormalizer.NormalizeEmail(request.GoogleEmail!)
            : null;
        if (googleEmail is not null && await _accounts.GoogleEmailExistsAsync(googleEmail, cancellationToken: cancellationToken))
        {
            return ApiResult<InternalAccountResult>.Fail("Google email already belongs to another account.", 409);
        }

        var roles = new List<(Role Role, AccountRoleScopeRequest Scope)>();
        foreach (var roleScope in request.Roles)
        {
            if (roleScope.OrganizationId != command.OrganizationId)
            {
                return ApiResult<InternalAccountResult>.Fail(
                    "Every account role must belong to the organization in the request route.", 403);
            }

            var role = await _accounts.GetRoleByCodeAsync(roleScope.RoleCode.Trim(), cancellationToken);
            if (role is null)
            {
                return ApiResult<InternalAccountResult>.Fail($"Role '{roleScope.RoleCode}' does not exist.", 400);
            }

            var authorizationError = AccountRoleAssignmentRules.ValidateRoleAssignmentPermission(
                command.UserContext,
                command.UserRoles,
                role.Code);
            if (authorizationError is not null)
            {
                return ApiResult<InternalAccountResult>.Fail(authorizationError, 403);
            }

            var scopeError = AccountRoleAssignmentRules.ValidateRequestedScope(
                command.UserContext,
                role.Code,
                roleScope);
            if (scopeError is not null)
            {
                return ApiResult<InternalAccountResult>.Fail(scopeError, 403);
            }

            roles.Add((role, roleScope));
        }

        var now = DateTimeOffset.UtcNow;
        var account = new Account
        {
            UserName = userName,
            Email = email,
            FullName = request.FullName?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Address = request.Address?.Trim(),
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? "Other" : request.Gender.Trim(),
            Status = AccountStatus.Active,
            LocalLoginEnabled = true,
            GoogleLoginEnabled = request.GoogleLoginEnabled,
            GoogleEmail = googleEmail,
            CreatedAt = now,
            CreatedByAccountId = createdByAccountId
        };

        foreach (var (role, scope) in roles)
        {
            account.AccountRoles.Add(new AccountRole
            {
                RoleId = role.Id,
                Role = role,
                OrganizationId = scope.OrganizationId,
                StoreId = scope.StoreId,
                KioskId = scope.KioskId,
                AssignedAt = now,
                AssignedByAccountId = createdByAccountId
            });
        }

        // Temporary override. AccountInvitationService and its endpoints remain available
        // for restoring token-based onboarding after the demo.
        var credentials = _credentials.Prepare(account, now);
        await _accounts.AddAsync(account, cancellationToken);
        await _accounts.SaveChangesAsync(cancellationToken);

        /* TARGET FLOW - replace temporary credential delivery with:
         * var invitationResult = await _invitationService.CreateInvitationAsync(
         *     account, createdByAccountId, request.SendInvitationEmail, cancellationToken);
         * if (!invitationResult.Succeeded || invitationResult.Data is null)
         *     return ApiResult<InternalAccountResult>.Fail(
         *         invitationResult.Message ?? "Invitation could not be created.",
         *         invitationResult.StatusCode);
         * return ApiResult<InternalAccountResult>.Success(
         *     InternalAccountResultMapper.ToResult(account, invitationResult.Data),
         *     invitationResult.Message ?? "Internal account invited.", 201);
         */
        var emailSent = await _credentials.TrySendAsync(account, credentials, cancellationToken);
        var message = emailSent
            ? "Internal account created and credentials emailed."
            : "Internal account created, but credentials email failed. Reset the password before handing over the account.";
        return ApiResult<InternalAccountResult>.Success(InternalAccountResultMapper.ToResult(account, null), message, 201);
    }
}
