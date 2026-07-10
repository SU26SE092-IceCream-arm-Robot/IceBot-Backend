using Infrastructure.RobotConfiguration.ArtifactTemplates.Persistence;
using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using System.Text;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Domain.Common.Enums;
using Domain.RobotConfiguration.Artifacts;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Concurrency;
using Infrastructure.Data;
using Infrastructure.RobotConfiguration.Storage.Jobs;
using Infrastructure.RobotConfiguration.Storage.ObjectStorage;
using Infrastructure.RobotConfiguration.Artifacts.Persistence;
using Infrastructure.ProductionConfiguration.ObjectStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.RobotConfiguration;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class OrphanCleanupIntegrationTests
{
    private readonly IntegrationTestFixture _fixture;

    public OrphanCleanupIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task Cleanup_RemovesUnreferencedObjectAndKeepsReferencedObject()
    {
        var storage = _fixture.CreateObjectStorage(autoCreateBucket: true);
        var orphanKey = $"robot-artifacts/orphan/{Guid.NewGuid():N}.lua";
        var referencedKey = $"robot-artifacts/referenced/{Guid.NewGuid():N}.lua";
        await WriteAsync(storage, orphanKey, "orphan");
        await WriteAsync(storage, referencedKey, "referenced");
        await using (var seedContext = _fixture.CreateDbContext())
        {
            var organization = new Organization
            {
                Code = $"ORG-{Guid.NewGuid():N}",
                Name = "Cleanup organization",
                Status = EntityStatus.Active
            };
            seedContext.Organizations.Add(organization);
            seedContext.RobotArtifacts.Add(RobotArtifact.CreateDraft(
                organization.Id,
                "REFERENCED",
                "Referenced artifact",
                referencedKey,
                "referenced.lua",
                new string('e', 64),
                "FAIRINO_LUA_V1",
                "FR5",
                Encoding.UTF8.GetByteCount("referenced"),
                DateTimeOffset.UtcNow));
            await seedContext.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddSingleton<IArtifactObjectStorage>(storage);
        services.AddScoped(_ => _fixture.CreateDbContext());
        services.AddScoped<IRobotArtifactStore, RobotArtifactStore>();
        services.AddScoped<IRobotArtifactTemplateStore, RobotArtifactTemplateStore>();
        services.AddScoped<IArtifactObjectReferenceSource, RobotConfigurationObjectReferenceSource>();
        services.AddScoped<IArtifactObjectReferenceSource, ConfigurationReleaseBundleReferenceSource>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CONNECTIONSTRING"] = _fixture.ConnectionString
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<PostgresAdvisoryLockManager>();
        await using var provider = services.BuildServiceProvider();
        var job = new RobotArtifactOrphanCleanupJob(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RobotArtifactObjectStorageOptions
            {
                OrphanCleanupEnabled = true,
                OrphanGracePeriodHours = -1,
                OrphanCleanupIntervalHours = 24,
                OrphanCleanupMaxDeletesPerRun = 100
            }),
            NullLogger<RobotArtifactOrphanCleanupJob>.Instance);

        await job.StartAsync(CancellationToken.None);
        await WaitUntilAsync(async () => !await storage.ExistsAsync(orphanKey), TimeSpan.FromSeconds(10));
        await job.StopAsync(CancellationToken.None);

        Assert.False(await storage.ExistsAsync(orphanKey));
        Assert.True(await storage.ExistsAsync(referencedKey));
    }

    private static async Task WriteAsync(IArtifactObjectStorage storage, string key, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await using var content = new MemoryStream(bytes);
        await storage.WriteImmutableAsync(
            new ArtifactObjectWriteRequest(key, "text/plain", bytes.Length, new string('e', 64)),
            content);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("The orphan cleanup condition was not reached before timeout.");
    }
}
