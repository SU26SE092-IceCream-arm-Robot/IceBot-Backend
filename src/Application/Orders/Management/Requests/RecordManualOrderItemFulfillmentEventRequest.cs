using System.ComponentModel.DataAnnotations;

namespace Application.Orders.Management.Requests;

public sealed class RecordManualOrderItemFulfillmentEventRequest
{
    [Required]
    public Guid FulfillmentEventId { get; init; }
    public ManualOrderItemFulfillmentEventType EventType { get; init; }
    [StringLength(500)]
    public string? Reason { get; init; }
}

public enum ManualOrderItemFulfillmentEventType
{
    Accepted = 1,
    Preparing = 2,
    Completed = 3,
    Failed = 4
}
