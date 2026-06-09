using System.Net;

namespace Application.Identity.Invitations;

internal static class AccountInvitationUrlBuilder
{
    public static string? BuildInvitationUrl(string rawToken, string? invitationBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(invitationBaseUrl))
        {
            return null;
        }

        var encodedToken = WebUtility.UrlEncode(rawToken);
        return $"{invitationBaseUrl.TrimEnd('/')}?token={encodedToken}";
    }
}
