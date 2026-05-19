namespace Application.Identity.Authentication.Requests
{
    public class RefreshAccessTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}

