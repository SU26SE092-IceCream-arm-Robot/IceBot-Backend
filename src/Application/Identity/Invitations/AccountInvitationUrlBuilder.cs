using System.Net;

namespace Application.Identity.Invitations;

internal static class AccountInvitationUrlBuilder
{
    public static string BuildInvitationUrl(string rawToken, string? invitationBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(invitationBaseUrl))
        {
            throw new InvalidOperationException("Email invitation base URL is not configured.");
        }

        var encodedToken = WebUtility.UrlEncode(rawToken);
        return $"{invitationBaseUrl.TrimEnd('/')}?token={encodedToken}";
    }
}
