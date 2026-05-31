using System.ComponentModel.DataAnnotations;

namespace Application.Identity.Authentication.Requests
{
    public class LoginAccountRequest
    {
        [Required]
        public string EmailOrUsername { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }
}
