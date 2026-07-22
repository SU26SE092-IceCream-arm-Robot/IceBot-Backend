using Infrastructure.RobotConfiguration.Storage.ObjectStorage;
using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using System.Text.Json;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.ProductionConfiguration.Enums;
using Domain.Sync.Enums;

namespace Infrastructure.ProductionConfiguration.ObjectStorage;

public sealed class ConfigurationReleaseBundleReferenceSource : IArtifactObjectReferenceSource
{
    private readonly IceBotDbContext _dbContext;

    public ConfigurationReleaseBundleReferenceSource(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<string>> ListReferencedStorageKeysAsync(
        CancellationToken cancellationToken = default)
    {
        var manifests = _dbContext.ConfigurationReleases.AsNoTracking()
            .Where(release => release.ManifestJson != null)
            .Select(release => release.ManifestJson!)
            .AsAsyncEnumerable();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        await foreach (var manifestJson in manifests.WithCancellation(cancellationToken))
        {
            AddBundleKey(manifestJson, keys);
        }

        var liveDeploymentIds = _dbContext.KioskConfigurationDeployments.AsNoTracking()
            .Where(deployment => deployment.Status == KioskConfigurationDeploymentStatus.Pending ||
                deployment.Status == KioskConfigurationDeploymentStatus.Installed)
            .Select(deployment => deployment.Id);
        var deploymentPayloads = _dbContext.EdgeCommands.AsNoTracking()
            .Where(command => command.DeploymentKind == DeploymentCommandTargetKind.FullEdgeConfiguration &&
                command.DeploymentId.HasValue && liveDeploymentIds.Contains(command.DeploymentId.Value))
            .Select(command => command.PayloadJson)
            .AsAsyncEnumerable();
        await foreach (var payloadJson in deploymentPayloads.WithCancellation(cancellationToken))
        {
            AddBundleKey(payloadJson, keys);
        }

        return keys;
    }

    private static void AddBundleKey(string json, ISet<string> keys)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("FullEdgeBundle", out var bundle) &&
                bundle.TryGetProperty("StorageKey", out var storageKey) &&
                !string.IsNullOrWhiteSpace(storageKey.GetString()))
            {
                keys.Add(storageKey.GetString()!);
            }
        }
        catch (JsonException)
        {
            // Invalid durable payloads are quarantined by command delivery.
        }
    }
}
