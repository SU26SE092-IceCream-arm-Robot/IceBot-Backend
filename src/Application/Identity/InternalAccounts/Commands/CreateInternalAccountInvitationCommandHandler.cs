using Application.Identity.Abstractions;
using Application.Identity.Invitations.Results;
using Application.Identity.Invitations.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Enums;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class CreateInternalAccountInvitationCommandHandler
{
    private readonly IIdentityAccountStore _accounts;
    private readonly AccountInvitationService _invitationService;

    public CreateInternalAccountInvitationCommandHandler(
        IIdentityAccountStore accounts,
        AccountInvitationService invitationService)
    {
        _accounts = accounts;
        _invitationService = invitationService;
    }

    public async Task<ApiResult<AccountInvitationResult>> HandleAsync(
        CreateInternalAccountInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        var accountId = command.AccountId;
        var invitedByAccountId = command.InvitedByAccountId;
        var sendEmail = command.SendEmail;

        var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<AccountInvitationResult>.Fail("Account not found.", 404);
        }

        if (account.Status != AccountStatus.Invited)
        {
            return ApiResult<AccountInvitationResult>.Fail("Invitation can only be created for accounts with Invited status.", 400);
        }

        var result = await _invitationService.CreateInvitationAsync(account, invitedByAccountId, sendEmail, cancellationToken);
        return result;
    }
}
