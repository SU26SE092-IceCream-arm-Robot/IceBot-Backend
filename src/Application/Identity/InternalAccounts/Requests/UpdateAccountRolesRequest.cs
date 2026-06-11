using System.ComponentModel.DataAnnotations;

namespace Application.Identity.InternalAccounts.Requests;

public sealed class UpdateAccountRolesRequest
{
    [Required]
    public List<AccountRoleScopeRequest> Roles { get; set; } = [];
}
