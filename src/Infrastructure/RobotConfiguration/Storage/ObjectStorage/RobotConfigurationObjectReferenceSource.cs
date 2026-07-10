using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.RobotConfiguration.Storage.ObjectStorage;

public sealed class RobotConfigurationObjectReferenceSource : IArtifactObjectReferenceSource
{
    private readonly IceBotDbContext _dbContext;

    public RobotConfigurationObjectReferenceSource(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<string>> ListReferencedStorageKeysAsync(
        CancellationToken cancellationToken = default)
    {
        var artifactKeys = await _dbContext.RobotArtifacts.AsNoTracking()
            .Select(artifact => artifact.StorageKey)
            .ToArrayAsync(cancellationToken);
        var templateKeys = await _dbContext.RobotArtifactTemplates.AsNoTracking()
            .Select(template => template.StorageKey)
            .ToArrayAsync(cancellationToken);
        return artifactKeys.Concat(templateKeys).Distinct(StringComparer.Ordinal).ToArray();
    }
}
