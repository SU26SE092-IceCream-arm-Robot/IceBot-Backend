using System.ComponentModel.DataAnnotations;

namespace Application.Orders.Management.Requests;

public sealed class RecordPackagedOrderItemFulfillmentRequest
{
    [Required]
    public Guid FulfillmentEventId { get; init; }

    [StringLength(500)]
    public string? Reason { get; init; }
}
