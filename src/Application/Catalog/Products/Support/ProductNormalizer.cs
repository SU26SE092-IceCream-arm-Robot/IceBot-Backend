using System;

namespace Application.Catalog.Products.Support;

internal static class ProductNormalizer
{
    public static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    public static string NormalizeOptionalCode(string? value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : NormalizeCode(value);
    }

    public static string? NormalizeNullableCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeCode(value);
    }

    public static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
