namespace Application.Tenants.Stores.Requests;

public sealed class StoreOpeningHoursDayRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }
}
