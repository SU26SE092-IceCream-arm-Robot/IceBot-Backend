namespace Application.Identity.InternalAccounts.Requests
{
    public class UpdateInternalAccountRequest
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public bool? LocalLoginEnabled { get; set; }
        public bool? GoogleLoginEnabled { get; set; }
        public string? GoogleEmail { get; set; }
    }
}
