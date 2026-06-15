using Domain.Operations.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.Operations.MaintenanceTickets.Requests;

public sealed class UpdateMaintenanceTicketRequest
{
    [Required]
    [StringLength(200)]
    public required string Title { get; init; }
    [StringLength(1000)]
    public string? Description { get; init; }
    public required MaintenancePriority Priority { get; init; }
    public Guid? DeviceId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? DeviceEventId { get; init; }
}
