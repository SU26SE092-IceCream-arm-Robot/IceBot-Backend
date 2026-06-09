using System.Text.RegularExpressions;

namespace Application.Identity.InternalAccounts;

internal static class InternalAccountNormalizer
{
    public static string NormalizeEmail(string value) => value.Trim().ToLowerInvariant();

    public static string NormalizeUserName(string value)
    {
        var normalized = Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9_.-]", string.Empty);
        return normalized.Length > 50 ? normalized[..50] : normalized;
    }
}
