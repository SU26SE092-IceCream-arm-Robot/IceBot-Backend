using Application.RobotConfiguration.Abstractions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.RobotConfiguration.ObjectStorage;

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
