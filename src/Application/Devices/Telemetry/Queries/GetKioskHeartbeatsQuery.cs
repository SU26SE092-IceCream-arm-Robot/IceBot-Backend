using Domain.Devices.Telemetry;
using Application.Identity.Tokens.Claims;
using Domain.Devices.Catalog;

namespace Application.Devices.Telemetry.Queries;

public sealed class GetKioskHeartbeatsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public KioskHeartbeatStatus? Status { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
