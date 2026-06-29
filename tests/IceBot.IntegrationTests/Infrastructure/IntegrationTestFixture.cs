using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Infrastructure.Data;
using Infrastructure.RobotConfiguration.ObjectStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace IceBot.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public const string CollectionName = "PostgreSQL and MinIO";
    private const string MinioAccessKey = "icebot-integration";
    private const string MinioSecretKey = "icebot-integration-secret";
    private const string BucketName = "icebot-integration-artifacts";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("IceBotIntegration")
        .WithUsername("postgres")
        .WithPassword("integration-password")
        .Build();

    private readonly IContainer _minio = new ContainerBuilder("minio/minio:RELEASE.2025-09-07T16-13-09Z")
        .WithEnvironment("MINIO_ROOT_USER", MinioAccessKey)
        .WithEnvironment("MINIO_ROOT_PASSWORD", MinioSecretKey)
        .WithPortBinding(9000, true)
        .WithCommand("server", "/data")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(
            request => request.ForPort(9000).ForPath("/minio/health/live")))
        .Build();

    private static bool Enabled => string.Equals(
        Environment.GetEnvironmentVariable("ICEBOT_RUN_INTEGRATION_TESTS"),
        "true",
        StringComparison.OrdinalIgnoreCase);

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        if (!Enabled)
        {
            return;
        }

        await Task.WhenAll(_postgres.StartAsync(), _minio.StartAsync());
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!Enabled)
        {
            return;
        }

        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _minio.DisposeAsync().AsTask());
    }

    public IceBotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IceBotDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new IceBotDbContext(options);
    }

    public MinioArtifactObjectStorage CreateObjectStorage(int downloadUrlExpirySeconds = 300)
    {
        var endpoint = $"{_minio.Hostname}:{_minio.GetMappedPublicPort(9000)}";
        return new MinioArtifactObjectStorage(Options.Create(new RobotArtifactObjectStorageOptions
        {
            Endpoint = endpoint,
            DownloadEndpoint = endpoint,
            AccessKey = MinioAccessKey,
            SecretKey = MinioSecretKey,
            BucketName = BucketName,
            UseSsl = false,
            DownloadUseSsl = false,
            DownloadUrlExpirySeconds = downloadUrlExpirySeconds
        }));
    }
}

[CollectionDefinition(IntegrationTestFixture.CollectionName, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>;
