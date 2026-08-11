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
        if (points.Any(point => point.Level == Domain.Inventory.Enums.IngredientLevelStatus.Unknown))
            return "Unknown cannot be configured in a level-to-quantity profile.";
        if (points.Any(point => point.EstimatedQuantity < 0))
            return "Dispenser profile quantities cannot be negative.";
        if (capacityQuantity.HasValue && points.Any(point => point.EstimatedQuantity > capacityQuantity.Value))
            return "Dispenser profile quantity cannot exceed capacity.";
        if (points.Count > 0)
        {
            var requiredLevels = new[]
            {
                Domain.Inventory.Enums.IngredientLevelStatus.Low,
                Domain.Inventory.Enums.IngredientLevelStatus.Medium,
                Domain.Inventory.Enums.IngredientLevelStatus.Full
            };
            if (points.Count != requiredLevels.Length || requiredLevels.Any(level => points.All(point => point.Level != level)))
                return "A level-to-quantity profile must define Low, Medium, and Full.";

            var ordered = points.OrderBy(point => point.Level).Select(point => point.EstimatedQuantity).ToArray();
            if (ordered[0] >= ordered[1] || ordered[1] >= ordered[2])
                return "Profile quantities must increase strictly from Low to Medium to Full.";
        }
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

    public static bool TryResolveEstimatedQuantity(
        string? json,
        Domain.Inventory.Enums.IngredientLevelStatus level,
        out decimal? estimatedQuantity)
    {
        estimatedQuantity = null;
        var point = Deserialize(json).FirstOrDefault(candidate => candidate.Level == level);
        if (point is null)
            return false;

        estimatedQuantity = point.EstimatedQuantity;
        return true;
    }
}
