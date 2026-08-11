namespace Infrastructure.SalesCatalog.Caching;

public sealed class RuntimeMenuCacheOptions
{
    public const string SectionName = "RuntimeMenuCache";

    public bool Enabled { get; init; }

    public string? RedisConnectionString { get; init; }

    public string InstanceName { get; init; } = string.Empty;

    public int DistributedExpirationSeconds { get; init; } = 10;

    public int LocalExpirationSeconds { get; init; } = 1;

    public int UncachedSnapshotExpirationSeconds { get; init; } = 15;
}
