using System.ComponentModel.DataAnnotations;

namespace Application.Identity.Authentication.Requests
{
    public class ExternalLoginRequest
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }
}
