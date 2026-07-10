namespace Infrastructure.Data.Configurations;

internal static class EfModelConfigurationConstants
{
    public const string ActiveRowFilter = "\"DeletedAt\" IS NULL";

    public static string NotNullAndActive(string columnName) =>
        $"\"{columnName}\" IS NOT NULL AND {ActiveRowFilter}";
}
