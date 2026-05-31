namespace Application.Identity.Authentication.Requests
{
    public class RevokeRefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}

