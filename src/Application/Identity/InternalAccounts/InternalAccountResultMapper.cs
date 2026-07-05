using Application.Identity.InternalAccounts.Results;
using Application.Identity.Invitations.Results;
using Domain.Identity.Entities;

namespace Application.Identity.InternalAccounts;

internal static class InternalAccountResultMapper
{
    public static InternalAccountResult ToResult(Account account, AccountInvitationResult? invitation = null)
    {
        return new InternalAccountResult
        {
            Id = account.Id,
            UserName = account.UserName,
            Email = account.Email,
            FullName = account.FullName,
            Status = account.Status.ToString(),
            LocalLoginEnabled = account.LocalLoginEnabled,
            GoogleLoginEnabled = account.GoogleLoginEnabled,
            GoogleEmail = account.GoogleEmail,
            Invitation = invitation is null
                ? null
                : new InternalAccountInvitationResult
                {
                    InvitationUrl = invitation.InvitationUrl,
                    ExpiresAt = invitation.ExpiresAt,
                    EmailSentAt = invitation.EmailSentAt
                },
            Roles = account.AccountRoles.Select(accountRole => new InternalAccountRoleResult
            {
                RoleCode = accountRole.Role.Code,
                OrganizationId = accountRole.OrganizationId,
                StoreId = accountRole.StoreId,
                KioskId = accountRole.KioskId
            }).ToList()
        };
    }
}
