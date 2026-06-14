using System;

namespace Application.Abstractions.Realtime.Events;

public sealed record DashboardInvalidatedEvent
{
    public string Type => "DashboardInvalidated";
    public required string Scope { get; init; } // System, Organization, Store
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public required string Reason { get; init; } // e.g. OrderStatusChanged, KioskStatusChanged, etc.
    public required DateTimeOffset UpdatedAt { get; init; }
}
