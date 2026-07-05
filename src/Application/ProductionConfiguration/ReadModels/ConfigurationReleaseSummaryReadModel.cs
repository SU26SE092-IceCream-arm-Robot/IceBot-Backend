namespace Application.ProductionConfiguration.ReadModels;

public sealed class ConfigurationReleaseSummaryReadModel
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public long ReleaseNumber { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ReleaseChecksum { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public Guid? PublishedByAccountId { get; init; }
    public int RouteCount { get; init; }
}
