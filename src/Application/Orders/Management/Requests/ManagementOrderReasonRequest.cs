using System.ComponentModel.DataAnnotations;

namespace Application.Orders.Management.Requests;

public sealed class ManagementOrderReasonRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
