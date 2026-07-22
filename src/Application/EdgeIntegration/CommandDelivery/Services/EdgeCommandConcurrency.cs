namespace Application.EdgeIntegration.CommandDelivery.Services;

public static class EdgeCommandConcurrency
{
    public static string CommandLockKey(Guid commandId) => $"execution-command:{commandId:D}";

    public static string EndpointDeliveryLockKey(Guid endpointId) => $"edge-command-pull:{endpointId:D}";
}
