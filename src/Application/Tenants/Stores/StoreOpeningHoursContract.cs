using System.Text.Json;
using Application.Tenants.Stores.Requests;
using Application.Tenants.Stores.Results;

namespace Application.Tenants.Stores;

internal static class StoreOpeningHoursContract
{
    public static string? Validate(IReadOnlyCollection<StoreOpeningHoursDayRequest> days)
    {
        if (days.GroupBy(day => day.DayOfWeek).Any(group => group.Count() > 1))
            return "Opening hours may contain only one entry per day.";

        foreach (var day in days)
        {
            if (day.IsClosed && (day.OpensAt.HasValue || day.ClosesAt.HasValue))
                return $"Closed day '{day.DayOfWeek}' must not contain opening or closing times.";
            if (!day.IsClosed && (!day.OpensAt.HasValue || !day.ClosesAt.HasValue))
                return $"Open day '{day.DayOfWeek}' requires opening and closing times.";
            if (!day.IsClosed && day.OpensAt!.Value >= day.ClosesAt!.Value)
                return $"Opening time must be earlier than closing time for '{day.DayOfWeek}'.";
        }

        return null;
    }

    public static string Serialize(IReadOnlyCollection<StoreOpeningHoursDayRequest> days) =>
        JsonSerializer.Serialize(days.OrderBy(day => day.DayOfWeek));

    public static IReadOnlyList<StoreOpeningHoursDayResult> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<StoreOpeningHoursDayResult>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
