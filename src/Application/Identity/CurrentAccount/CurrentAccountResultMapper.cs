using Application.Identity.CurrentAccount.Results;
using Domain.Identity.Entities;

namespace Application.Identity.CurrentAccount;

internal static class CurrentAccountResultMapper
{
    public static CurrentAccountResult ToResult(Account account)
    {
        return new CurrentAccountResult
        {
            Id = account.Id,
            UserName = account.UserName,
            Email = account.Email,
            EmailConfirmed = account.EmailConfirmed,
            FullName = account.FullName,
            ImageUrl = account.ImageUrl,
            PhoneNumber = account.PhoneNumber,
            PhoneNumberConfirmed = account.PhoneNumberConfirmed,
            Address = account.Address,
            Gender = account.Gender,
            Status = account.Status.ToString(),
            LocalLoginEnabled = account.LocalLoginEnabled,
            GoogleLoginEnabled = account.GoogleLoginEnabled,
            GoogleEmail = account.GoogleEmail,
            LastLoginAt = account.LastLoginAt,
            Roles = account.AccountRoles
                .Where(accountRole => accountRole.IsActive)
                .OrderBy(accountRole => accountRole.Role.Priority)
                .Select(accountRole => new CurrentAccountRoleResult
                {
                    RoleCode = accountRole.Role.Code,
                    OrganizationId = accountRole.OrganizationId,
                    StoreId = accountRole.StoreId,
                    KioskId = accountRole.KioskId
                })
                .ToList()
        };
    }
}
