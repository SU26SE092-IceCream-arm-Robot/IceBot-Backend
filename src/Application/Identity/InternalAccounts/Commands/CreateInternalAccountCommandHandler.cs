using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Requests;
using Application.Identity.InternalAccounts.Results;
using Application.Identity.Invitations.Results;
using Application.Identity.Invitations.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class CreateInternalAccountCommandHandler
{
    private readonly IIdentityAccountStore _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AccountInvitationService _invitationService;

    public CreateInternalAccountCommandHandler(
        IIdentityAccountStore accounts,
        IPasswordHasher passwordHasher,
        AccountInvitationService invitationService)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _invitationService = invitationService;
    }

    public async Task<ApiResult<InternalAccountResult>> HandleAsync(
        CreateInternalAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var createdByAccountId = command.CreatedByAccountId;

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

        var roles = new List<(Role Role, AccountRoleScopeRequest Scope)>();
        foreach (var roleScope in request.Roles)
        {
            var role = await _accounts.GetRoleByCodeAsync(roleScope.RoleCode.Trim(), cancellationToken);
            if (role is null)
            {
                return ApiResult<InternalAccountResult>.Fail($"Role '{roleScope.RoleCode}' does not exist.", 400);
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
            Status = request.CreateInvitation ? AccountStatus.Invited : AccountStatus.Active,
            LocalLoginEnabled = request.LocalLoginEnabled,
            GoogleLoginEnabled = request.GoogleLoginEnabled,
            GoogleEmail = request.GoogleLoginEnabled ? InternalAccountNormalizer.NormalizeEmail(request.GoogleEmail!) : null,
            Password = (request.LocalLoginEnabled && !request.CreateInvitation && !string.IsNullOrWhiteSpace(request.InitialPassword))
                ? HashedPassword.From(_passwordHasher.HashPassword(request.InitialPassword))
                : null,
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

        await _accounts.AddAsync(account, cancellationToken);
        await _accounts.SaveChangesAsync(cancellationToken);

        AccountInvitationResult? invitation = null;
        string message = "Internal account created.";

        if (request.CreateInvitation)
        {
            var invitationResult = await _invitationService.CreateInvitationAsync(
                account,
                createdByAccountId,
                request.SendInvitationEmail,
                cancellationToken);

            if (!invitationResult.Succeeded || invitationResult.Data is null)
            {
                return ApiResult<InternalAccountResult>.Fail(
                    invitationResult.Message ?? "Invitation could not be created.",
                    invitationResult.StatusCode);
            }

            invitation = invitationResult.Data;
            message = invitationResult.Message ?? message;
        }

        return ApiResult<InternalAccountResult>.Success(InternalAccountResultMapper.ToResult(account, invitation), message, 201);
    }
}
