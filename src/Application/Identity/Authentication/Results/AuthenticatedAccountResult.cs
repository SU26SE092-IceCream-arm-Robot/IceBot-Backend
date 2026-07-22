namespace Application.Identity.Authentication.Results
{
    public class AuthenticatedAccountResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<AuthenticatedAccountRoleResult> Roles { get; set; } = [];
        public bool LocalLoginEnabled { get; set; }
        public bool GoogleLoginEnabled { get; set; }
    }

    public class AuthenticatedAccountRoleResult
    {
        public string RoleCode { get; set; } = string.Empty;
        public Guid? OrganizationId { get; set; }
        public Guid? StoreId { get; set; }
        public Guid? KioskId { get; set; }
    }
}
