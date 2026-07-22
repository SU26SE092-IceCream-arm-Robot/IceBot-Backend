using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.AuthoringImports.Rules;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.RobotConfiguration.Storage.ObjectStorage;

public sealed class RobotConfigurationObjectReferenceSource : IArtifactObjectReferenceSource
{
    private readonly IceBotDbContext _dbContext;
    private readonly RobotArtifactObjectStorageOptions _options;

    public RobotConfigurationObjectReferenceSource(IceBotDbContext dbContext, IOptions<RobotArtifactObjectStorageOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
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
        var importThreshold = DateTimeOffset.UtcNow.AddHours(-_options.AuthoringImportRetentionHours);
        var activeImportKeys = await _dbContext.RobotAuthoringImports.AsNoTracking()
            .Where(RobotAuthoringImportStagingRetentionPolicy.BuildPredicate(importThreshold))
            .Select(importSession => importSession.StagingStorageKey)
            .ToArrayAsync(cancellationToken);
        return artifactKeys.Concat(templateKeys).Concat(activeImportKeys)
            .Distinct(StringComparer.Ordinal).ToArray();
    }
}
