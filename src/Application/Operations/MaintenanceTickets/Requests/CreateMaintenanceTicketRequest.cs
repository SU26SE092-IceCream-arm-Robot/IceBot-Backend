using Domain.Operations.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.Operations.MaintenanceTickets.Requests;

public sealed class CreateMaintenanceTicketRequest
{
    public Guid KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? DeviceEventId { get; init; }
    [Required]
    [StringLength(200)]
    public required string Title { get; init; }
    [StringLength(1000)]
    public string? Description { get; init; }
    [StringLength(100)]
    public string? IssueCode { get; init; }
    public MaintenancePriority? Priority { get; init; }
}
