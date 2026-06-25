using Application.RobotConfiguration.Abstractions;
using Domain.RobotConfiguration.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.RobotConfiguration.Persistence;

public sealed class RobotConfigurationStore : IRobotConfigurationStore
{
    private readonly IceBotDbContext _dbContext;

    public RobotConfigurationStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RobotArtifact?> GetArtifactForPublishAsync(Guid artifactId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotArtifacts
            .FirstOrDefaultAsync(artifact => artifact.Id == artifactId, cancellationToken);
    }

    public Task<RobotProgram?> GetProgramForPublishAsync(Guid programId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotPrograms
            .Include(program => program.RobotProgramArtifacts)
                .ThenInclude(programArtifact => programArtifact.RobotArtifact)
            .FirstOrDefaultAsync(program => program.Id == programId, cancellationToken);
    }

    public Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.AnyAsync(
            organization => organization.Id == organizationId && organization.DeletedAt == null,
            cancellationToken);
    }

    public Task<bool> ArtifactExistsAsync(
        Guid organizationId,
        string artifactCode,
        string checksum,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotArtifacts.AnyAsync(
            artifact => artifact.OrganizationId == organizationId &&
                artifact.ArtifactCode == artifactCode &&
                artifact.Checksum == checksum &&
                artifact.DeletedAt == null,
            cancellationToken);
    }

    public Task AddArtifactAsync(RobotArtifact artifact, CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotArtifacts.AddAsync(artifact, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
