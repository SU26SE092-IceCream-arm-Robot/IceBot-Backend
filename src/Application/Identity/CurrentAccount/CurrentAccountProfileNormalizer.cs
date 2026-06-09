namespace Application.Identity.CurrentAccount;

internal static class CurrentAccountProfileNormalizer
{
    public static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
