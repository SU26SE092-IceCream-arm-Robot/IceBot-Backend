namespace Application.Identity.InternalAccounts.Results
{
    public class InternalAccountResult
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool LocalLoginEnabled { get; set; }
        public bool GoogleLoginEnabled { get; set; }
        public InternalAccountInvitationResult? Invitation { get; set; }
        public List<InternalAccountRoleResult> Roles { get; set; } = [];
    }

    public class InternalAccountInvitationResult
    {
        public string InvitationToken { get; set; } = string.Empty;
        public string? InvitationUrl { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public bool EmailSent { get; set; }
    }

    public class InternalAccountRoleResult
    {
        public string RoleCode { get; set; } = string.Empty;
        public Guid? OrganizationId { get; set; }
        public Guid? StoreId { get; set; }
        public Guid? KioskId { get; set; }
    }
}
