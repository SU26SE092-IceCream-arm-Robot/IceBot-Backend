namespace Application.Operations.MaintenanceTickets.Requests;

public sealed class ResolveMaintenanceTicketRequest
{
    public required string ResolutionNotes { get; init; }
}
