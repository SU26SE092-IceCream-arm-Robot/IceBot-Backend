namespace Application.EdgeIntegration.Dispatch;

public sealed class OrderExecutionDispatchOptions
{
    public const string SectionName = "OrderExecutionDispatch";

    public bool Enabled { get; set; } = true;
    public int CommandExpiryMinutes { get; set; } = 30;
    public int MaxActiveCommandsPerEndpoint { get; set; } = 20;
    public int ReconciliationIntervalSeconds { get; set; } = 10;
    public int ReconciliationBatchSize { get; set; } = 50;
    public int InitialDispatchSupportEscalationMinutes { get; set; } = 15;
    public int TimeoutReconciliationIntervalSeconds { get; set; } = 30;
    public int TimeoutReconciliationBatchSize { get; set; } = 100;
    public int AcceptedReportTimeoutMinutes { get; set; } = 5;
    public int RunningReportTimeoutMinutes { get; set; } = 30;
    public int HeartbeatUnreachableMinutes { get; set; } = 2;
    public int UnreachableSupportEscalationMinutes { get; set; } = 15;
    public int MaxDispatchAttempts { get; set; } = 3;
}
