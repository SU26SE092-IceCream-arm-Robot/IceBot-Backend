namespace Application.Operations.MaintenanceTickets.Requests;

public sealed class AssignMaintenanceTicketRequest
{
    public Guid AssignedToAccountId { get; init; }
}
