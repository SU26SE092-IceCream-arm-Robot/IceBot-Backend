namespace Domain.Tenants.Enums;

public enum KioskOperationalState
{
    Operational = 1,
    PausedByOperator = 2,
    Maintenance = 3,
    Cleaning = 4,
    Restocking = 5,
    // Cloud-side hold/request; only Edge safety evidence may confirm a physical emergency stop.
    EmergencyStopRequested = 6,
    OutOfService = 7
}
