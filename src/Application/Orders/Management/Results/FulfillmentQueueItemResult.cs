using Domain.Catalog.Enums;
using Domain.Orders.Enums;

namespace Application.Orders.Management.Results;

public sealed record FulfillmentQueueItemResult(
    Guid OrderId,
    string OrderNumber,
    Guid OrderItemId,
    Guid KioskId,
    string ProductName,
    string ProductVariantName,
    int Quantity,
    FulfillmentType FulfillmentType,
    OrderItemStatus ItemStatus,
    DateTimeOffset? PaidAt,
    int? PreparationTimeSeconds,
    DateTimeOffset? ExpectedReadyAt,
    FulfillmentSlaStatus SlaStatus,
    IReadOnlyCollection<FulfillmentQueueOptionResult> SelectedOptions);

public sealed record FulfillmentQueueOptionResult(string GroupCode, string Code, string Name);

public enum FulfillmentSlaStatus
{
    NotConfigured = 0,
    OnTrack = 1,
    DueSoon = 2,
    Overdue = 3,
    Terminal = 4
}
