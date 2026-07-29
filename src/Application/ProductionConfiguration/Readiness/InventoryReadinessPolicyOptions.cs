namespace Application.ProductionConfiguration.Readiness;

public sealed class InventoryReadinessPolicyOptions
{
    public const string SectionName = "ProductionInventoryReadiness";

    public InventoryReadinessPolicy PublishPolicy { get; set; } = InventoryReadinessPolicy.Warn;
    public InventoryReadinessPolicy DeployPolicy { get; set; } = InventoryReadinessPolicy.Block;
}

public enum InventoryReadinessPolicy
{
    Warn = 1,
    Block = 2
}
