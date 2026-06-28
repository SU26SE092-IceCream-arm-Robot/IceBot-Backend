namespace Application.ProductionConfiguration;

public sealed class LowCostControllerCapacityOptions
{
    public const string SectionName = "LowCostControllerCapacity";

    public int MaxArtifactCount { get; init; } = 50;
    public long MaxArtifactStorageBytes { get; init; } = 52_428_800;
}
