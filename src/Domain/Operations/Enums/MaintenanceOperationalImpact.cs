namespace Domain.Operations.Enums;

public enum MaintenanceOperationalImpact
{
    None = 1,
    BlocksNewOrders = 2,
    // Requests intervention and holds new work; it does not confirm that hardware stopped.
    RequestsEmergencyStop = 3
}
