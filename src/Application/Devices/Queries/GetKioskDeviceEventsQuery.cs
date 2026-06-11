using Application.Identity.Tokens.Claims;
using Domain.Common.Enums;

namespace Application.Devices.Queries;

public sealed class GetKioskDeviceEventsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public SeverityLevel? MinSeverity { get; init; }
    public string? EventType { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
