namespace Domain.ProductionExecution.Enums;

public enum ProductionExecutionStatus
{
    Accepted = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    RequiresManualIntervention = 5
}
