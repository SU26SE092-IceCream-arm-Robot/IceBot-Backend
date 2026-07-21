using Domain.Tenants.Entities;
using Application.Tenants.Stores.Results;

namespace Application.Tenants.Stores;

public static class StoreSalesAvailabilityRules
{
    public static string? ValidateSalesAdmission(Store store, DateTimeOffset now)
    {
        if (store.IsSalesPausedAt(now))
        {
            return "Store is temporarily not accepting new orders.";
        }

        return ValidateOpeningHours(store, now);
    }

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
                    : !day.OpensAt.HasValue || !day.ClosesAt.HasValue || day.OpensAt.Value == day.ClosesAt.Value))
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
        var localTime = TimeOnly.FromDateTime(localNow.DateTime);
        return IsOpenAtLocalTime(schedule, localNow.DayOfWeek, localTime)
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

    private static bool IsOpenAtLocalTime(
        IReadOnlyList<StoreOpeningHoursDayResult> schedule,
        DayOfWeek dayOfWeek,
        TimeOnly localTime)
    {
        var today = schedule.SingleOrDefault(day => day.DayOfWeek == dayOfWeek);
        if (IsOpenDuringOwnDay(today, localTime))
        {
            return true;
        }

        var previousDay = dayOfWeek == DayOfWeek.Sunday
            ? DayOfWeek.Saturday
            : dayOfWeek - 1;
        var yesterday = schedule.SingleOrDefault(day => day.DayOfWeek == previousDay);

        return yesterday is { IsClosed: false, OpensAt: not null, ClosesAt: not null } &&
               yesterday.OpensAt.Value > yesterday.ClosesAt.Value &&
               localTime < yesterday.ClosesAt.Value;
    }

    private static bool IsOpenDuringOwnDay(StoreOpeningHoursDayResult? day, TimeOnly localTime)
    {
        if (day is not { IsClosed: false, OpensAt: not null, ClosesAt: not null })
        {
            return false;
        }

        return day.OpensAt.Value < day.ClosesAt.Value
            ? localTime >= day.OpensAt.Value && localTime < day.ClosesAt.Value
            : localTime >= day.OpensAt.Value;
    }
}
