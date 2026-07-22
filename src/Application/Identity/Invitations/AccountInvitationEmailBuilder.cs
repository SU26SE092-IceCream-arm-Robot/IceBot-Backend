using System.Net;

namespace Application.Identity.Invitations;

internal static class AccountInvitationEmailBuilder
{
    public static string BuildInvitationEmail(string? fullName, string invitationUrl)
    {
        var displayName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName.Trim());
        var inviteLink = $"""<p><a href="{WebUtility.HtmlEncode(invitationUrl)}">Set up your account</a></p>""";

        return $"""
            <p>Hi {displayName},</p>
            <p>You have been invited to join IceBot. Use the link below to complete your account setup. This link is valid for 7 days.</p>
            {inviteLink}
            <p>If you did not expect this invitation, you can safely ignore this email.</p>
            """;
    }
}
