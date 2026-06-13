namespace Application.Operations.MaintenanceTickets.Requests;

public sealed class CancelMaintenanceTicketRequest
{
    public required string Reason { get; init; }
}
