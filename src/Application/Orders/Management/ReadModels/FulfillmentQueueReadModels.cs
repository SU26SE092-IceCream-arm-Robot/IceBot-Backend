using Domain.Catalog.Enums;
using Domain.Orders.Enums;

namespace Application.Orders.Management.ReadModels;

public sealed record FulfillmentQueueItemReadModel(
    Guid OrderId,
    string OrderNumber,
    Guid OrderItemId,
    Guid? OrganizationId,
    Guid? StoreId,
    Guid KioskId,
    string ProductName,
    string ProductVariantName,
    int Quantity,
    FulfillmentType FulfillmentType,
    OrderItemStatus ItemStatus,
    DateTimeOffset? PaidAt,
    int? PreparationTimeSeconds,
    IReadOnlyCollection<FulfillmentQueueOptionReadModel> SelectedOptions);

public sealed record FulfillmentQueueOptionReadModel(string GroupCode, string Code, string Name);

public sealed record OrderItemOwnerReadModel(
    Guid OrderId,
    Guid OrderItemId,
    Guid? OrganizationId,
    Guid? StoreId,
    Guid KioskId);

public sealed record OrderItemStatusHistoryReadModel(
    Guid Id,
    Guid OrderItemId,
    Guid? SourceEventId,
    Guid? ChangedByAccountId,
    string? ChangedByName,
    string? ChangedByEmail,
    OrderItemStatus FromStatus,
    OrderItemStatus ToStatus,
    string? Reason,
    DateTimeOffset ChangedAt);
