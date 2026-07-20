using System.Security.Cryptography;
using System.Text;

namespace Application.ProductionPackages.Installation;

public static class ProductionPackageMaterializationCode
{
    public static string WithSuffix(string code, string? suffix, int maximumLength = 100)
    {
        var normalized = code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(suffix)) return normalized;
        var normalizedSuffix = suffix.Trim().ToUpperInvariant();
        var maximumBaseLength = maximumLength - normalizedSuffix.Length - 1;
        if (maximumBaseLength < 10)
            throw new ArgumentException("Materialization suffix leaves no room for a stable code identity.", nameof(suffix));
        if (normalized.Length <= maximumBaseLength)
            return $"{normalized}_{normalizedSuffix}";

        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))
            .ToLowerInvariant()[..8].ToUpperInvariant();
        var prefixLength = maximumBaseLength - fingerprint.Length - 1;
        return $"{normalized[..prefixLength]}_{fingerprint}_{normalizedSuffix}";
    }
}
