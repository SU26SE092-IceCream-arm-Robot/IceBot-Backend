using Infrastructure.RobotConfiguration.Storage.ObjectStorage;
using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using System.Text.Json;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            try
            {
                using var document = JsonDocument.Parse(manifestJson);
                if (document.RootElement.TryGetProperty("FullEdgeBundle", out var bundle) &&
                    bundle.TryGetProperty("StorageKey", out var storageKey) &&
                    !string.IsNullOrWhiteSpace(storageKey.GetString()))
                {
                    keys.Add(storageKey.GetString()!);
                }
            }
            catch (JsonException)
            {
                // Invalid release manifests are reported by release/deployment validation.
            }
        }

        return keys;
    }
}
