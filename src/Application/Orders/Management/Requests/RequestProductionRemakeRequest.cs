using System.ComponentModel.DataAnnotations;

namespace Application.Orders.Management.Requests;

public sealed class RequestProductionRemakeRequest
{
    public Guid RemakeRequestId { get; init; }
    [Range(1, int.MaxValue)]
    public int ProductionUnitNo { get; init; }
    [Range(1, int.MaxValue)]
    public int ProductionUnitQuantity { get; init; }
    [Required, StringLength(500)]
    public string Reason { get; init; } = string.Empty;
}
