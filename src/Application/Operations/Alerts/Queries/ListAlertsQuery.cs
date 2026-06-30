using Application.Identity.Tokens.Claims;

namespace Application.Operations.Alerts.Queries;

public sealed class ListAlertsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public string? Status { get; init; }
    public string? Severity { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
