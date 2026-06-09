using Application.Email;
using System.Net;

namespace Application.Identity.PasswordReset;

internal static class PasswordResetEmailBuilder
{
    public static string BuildResetEmail(string? fullName, string rawToken, EmailOptions emailOptions)
    {
        var encodedToken = WebUtility.UrlEncode(rawToken);
        var resetUrl = string.IsNullOrWhiteSpace(emailOptions.PasswordResetBaseUrl)
            ? null
            : $"{emailOptions.PasswordResetBaseUrl.TrimEnd('/')}?token={encodedToken}";

        var displayName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName.Trim());
        var tokenText = WebUtility.HtmlEncode(rawToken);
        var resetLink = resetUrl is null
            ? string.Empty
            : $"""<p><a href="{WebUtility.HtmlEncode(resetUrl)}">Reset password</a></p>""";

        return $"""
            <p>Hi {displayName},</p>
            <p>Use the token below to reset your IceBot password. This token expires in 30 minutes.</p>
            <p><strong>{tokenText}</strong></p>
            {resetLink}
            <p>If you did not request this, ignore this email.</p>
            """;
    }
}
