namespace Application.SalesCatalog.Menus.Support;

internal static class MenuNormalizer
{
    public static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    public static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
