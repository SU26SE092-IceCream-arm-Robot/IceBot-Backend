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
    IReadOnlyCollection<FulfillmentQueueOptionResult> SelectedOptions);

public sealed record FulfillmentQueueOptionResult(string GroupCode, string Code, string Name);
