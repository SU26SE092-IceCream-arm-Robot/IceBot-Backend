namespace Application.Identity.InternalAccounts.Requests
{
    public class SetInternalAccountPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
        public bool EnableLocalLogin { get; set; } = true;
    }
}
