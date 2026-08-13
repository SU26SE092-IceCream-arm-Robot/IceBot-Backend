using Application.Identity.Tokens.Claims;
using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Availability;

public sealed class KioskMenuItemAvailabilityResult
{
    public Guid KioskId { get; init; }
    public Guid MenuId { get; init; }
    public Guid MenuItemId { get; init; }
    public string DisplayName { get; init; } = null!;
    public string MenuName { get; init; } = null!;
    public bool CatalogSellable { get; init; }
    public MenuItemOperationalAvailabilityState State { get; init; }
    public MenuItemOperationalAvailabilityReasonCode? ReasonCode { get; init; }
    public string? Reason { get; init; }
    public long Revision { get; init; }
    public DateTimeOffset? ChangedAt { get; init; }
    public Guid? ChangedByAccountId { get; init; }
}

public sealed class SetKioskMenuItemAvailabilityRequest
{
    public MenuItemOperationalAvailabilityState State { get; init; }
    public MenuItemOperationalAvailabilityReasonCode ReasonCode { get; init; }
    public string? Reason { get; init; }
    public long ExpectedRevision { get; init; }
    public string? RequestId { get; init; }
}

public sealed class ListKioskMenuItemAvailabilityQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid KioskId { get; init; }
    public string? Search { get; init; }
    public MenuItemOperationalAvailabilityState? State { get; init; }
}

public sealed class SetKioskMenuItemAvailabilityCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid KioskId { get; init; }
    public Guid MenuItemId { get; init; }
    public required SetKioskMenuItemAvailabilityRequest Request { get; init; }
}

public sealed record KioskMenuItemAvailabilitySnapshot(
    Guid KioskId,
    Guid MenuId,
    Guid MenuItemId,
    MenuItemOperationalAvailabilityState State,
    long Revision,
    MenuItemOperationalAvailabilityReasonCode ReasonCode,
    string Reason,
    DateTimeOffset ChangedAt,
    Guid ChangedByAccountId);

public sealed record KioskMenuItemAvailabilityRequestReplay(
    Guid KioskId,
    Guid MenuId,
    Guid MenuItemId,
    MenuItemOperationalAvailabilityState RequestedState,
    MenuItemOperationalAvailabilityReasonCode RequestedReasonCode,
    string RequestedReason,
    long AppliedRevision,
    DateTimeOffset AppliedAt,
    Guid AppliedByAccountId);

public interface IMenuItemOperationalAvailabilityReader
{
    Task<IReadOnlySet<Guid>> GetPausedMenuItemIdsAsync(
        Guid kioskId,
        IReadOnlyCollection<Guid> menuItemIds,
        CancellationToken cancellationToken = default);

    Task<bool> IsPausedAsync(
        Guid kioskId,
        Guid menuItemId,
        CancellationToken cancellationToken = default);
}
