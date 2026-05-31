using System.ComponentModel.DataAnnotations;

namespace Application.Identity.InternalAccounts.Requests
{
    public class AccountRoleScopeRequest
    {
        [Required]
        public string RoleCode { get; set; } = null!;

        public Guid? OrganizationId { get; set; }

        public Guid? StoreId { get; set; }

        public Guid? KioskId { get; set; }
    }
}
