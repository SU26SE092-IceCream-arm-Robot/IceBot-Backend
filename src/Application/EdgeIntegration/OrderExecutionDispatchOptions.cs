namespace Application.EdgeIntegration;

public sealed class OrderExecutionDispatchOptions
{
    public const string SectionName = "OrderExecutionDispatch";

    public bool Enabled { get; set; } = true;
    public int CommandExpiryMinutes { get; set; } = 30;
    public int MaxActiveCommandsPerEndpoint { get; set; } = 20;
    public int ReconciliationIntervalSeconds { get; set; } = 10;
    public int ReconciliationBatchSize { get; set; } = 50;
}
