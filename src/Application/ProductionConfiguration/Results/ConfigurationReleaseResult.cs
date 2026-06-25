using Domain.ProductionConfiguration.Entities;

namespace Application.ProductionConfiguration.Results;

public sealed class ConfigurationReleaseResult
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public long ReleaseNumber { get; init; }
    public string Status { get; init; } = null!;
    public int ReleaseManifestSchemaVersion { get; init; }
    public string? ReleaseChecksum { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public Guid? PublishedByAccountId { get; init; }
    public int RouteCount { get; init; }

    public static ConfigurationReleaseResult FromEntity(ConfigurationRelease release)
    {
        return new ConfigurationReleaseResult
        {
            Id = release.Id,
            OrganizationId = release.OrganizationId,
            ReleaseNumber = release.ReleaseNumber,
            Status = release.Status.ToString(),
            ReleaseManifestSchemaVersion = release.ReleaseManifestSchemaVersion,
            ReleaseChecksum = release.ReleaseChecksum,
            PublishedAt = release.PublishedAt,
            PublishedByAccountId = release.PublishedByAccountId,
            RouteCount = release.ExecutionRoutes.Count
        };
    }
}
