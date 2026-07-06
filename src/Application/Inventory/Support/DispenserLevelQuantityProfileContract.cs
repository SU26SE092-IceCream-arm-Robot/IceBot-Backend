using System.Text.Json;
using Application.Inventory.Requests;
using Application.Inventory.Results;

namespace Application.Inventory.Support;

internal static class DispenserLevelQuantityProfileContract
{
    public static string? Validate(
        IReadOnlyCollection<DispenserLevelQuantityPointRequest> points,
        decimal? capacityQuantity)
    {
        if (points.Select(point => point.Level).Distinct().Count() != points.Count)
            return "Dispenser level-to-quantity profile cannot contain duplicate levels.";
        if (points.Any(point => point.EstimatedQuantity < 0))
            return "Dispenser profile quantities cannot be negative.";
        if (capacityQuantity.HasValue && points.Any(point => point.EstimatedQuantity > capacityQuantity.Value))
            return "Dispenser profile quantity cannot exceed capacity.";
        return null;
    }

    public static string? Serialize(IReadOnlyCollection<DispenserLevelQuantityPointRequest> points) =>
        points.Count == 0 ? null : JsonSerializer.Serialize(points.OrderBy(point => point.Level));

    public static IReadOnlyList<DispenserLevelQuantityPointResult> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<DispenserLevelQuantityPointResult>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
