using Domain.Tenants.Entities;

namespace Application.Tenants.Stores;

internal static class StoreSalesAvailabilityRules
{
    public static string? ValidateOpeningHours(Store store, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(store.OpeningHoursJson))
            return null;

        if (!StoreOpeningHoursContract.TryDeserialize(store.OpeningHoursJson, out var schedule))
            return "Store opening hours configuration is invalid.";

        // An empty schedule means the store has no configured sales-hour restriction.
        if (schedule.Count == 0)
            return null;

        if (schedule.GroupBy(day => day.DayOfWeek).Any(group => group.Count() > 1) ||
            schedule.Any(day =>
                day.IsClosed
                    ? day.OpensAt.HasValue || day.ClosesAt.HasValue
                    : !day.OpensAt.HasValue || !day.ClosesAt.HasValue || day.OpensAt.Value >= day.ClosesAt.Value))
        {
            return "Store opening hours configuration is invalid.";
        }

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(store.TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return "Store time zone configuration is invalid.";
        }
        catch (InvalidTimeZoneException)
        {
            return "Store time zone configuration is invalid.";
        }

        var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
        var today = schedule.SingleOrDefault(day => day.DayOfWeek == localNow.DayOfWeek);
        if (today is null || today.IsClosed || !today.OpensAt.HasValue || !today.ClosesAt.HasValue)
            return "Store is currently closed.";

        var localTime = TimeOnly.FromDateTime(localNow.DateTime);
        return localTime >= today.OpensAt.Value && localTime < today.ClosesAt.Value
            ? null
            : "Store is currently closed.";
    }

    public static string? ValidateTimeZone(string timeZoneId)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return null;
        }
        catch (TimeZoneNotFoundException)
        {
            return $"Time zone '{timeZoneId}' is not supported.";
        }
        catch (InvalidTimeZoneException)
        {
            return $"Time zone '{timeZoneId}' is invalid.";
        }
    }
}
