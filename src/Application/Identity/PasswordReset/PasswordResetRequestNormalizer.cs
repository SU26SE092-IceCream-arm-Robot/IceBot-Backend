namespace Application.Identity.PasswordReset;

internal static class PasswordResetRequestNormalizer
{
    public static string NormalizeEmailOrUserName(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Contains('@', StringComparison.Ordinal)
            ? trimmed.ToLowerInvariant()
            : trimmed.ToLowerInvariant();
    }
}
