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

    public Task<RobotProgram?> GetProgramForPublishAsync(Guid programId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotPrograms
            .Include(program => program.RobotProgramArtifacts)
                .ThenInclude(programArtifact => programArtifact.RobotArtifact)
            .FirstOrDefaultAsync(program => program.Id == programId, cancellationToken);
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
