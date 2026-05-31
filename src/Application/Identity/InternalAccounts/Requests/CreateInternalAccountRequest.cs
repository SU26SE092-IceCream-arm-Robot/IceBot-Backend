using System.ComponentModel.DataAnnotations;

namespace Application.Identity.InternalAccounts.Requests
{
    public class CreateInternalAccountRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        public string? FullName { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string Gender { get; set; } = "Other";

        public bool LocalLoginEnabled { get; set; }

        [DataType(DataType.Password)]
        public string? InitialPassword { get; set; }

        public bool GoogleLoginEnabled { get; set; }

        [EmailAddress]
        public string? GoogleEmail { get; set; }

        [MinLength(1)]
        public List<AccountRoleScopeRequest> Roles { get; set; } = [];
    }
}
