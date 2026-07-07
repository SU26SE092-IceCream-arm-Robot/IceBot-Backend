using System.Text.Json;

namespace Application.Devices.Support;

public static class DeviceCapabilityContract
{
    public const string IngredientDispenser = "IngredientDispenser";

    public static IReadOnlyList<string> Deserialize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(value) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(IReadOnlyList<string>? capabilities)
    {
        var normalized = (capabilities ?? [])
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return normalized.Length == 0 ? null : JsonSerializer.Serialize(normalized);
    }

    public static bool Supports(string? capabilitiesJson, string capability) =>
        Deserialize(capabilitiesJson).Contains(capability, StringComparer.OrdinalIgnoreCase);
}
