namespace Application.RobotConfiguration.ReadModels;

public sealed class RobotProgramSummaryReadModel
{
    public Guid Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ScopeType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int ProgramManifestSchemaVersion { get; init; }
    public string? ProgramManifestChecksum { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public int ArtifactCount { get; init; }
}
