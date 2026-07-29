using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.RobotConfiguration.ArtifactTemplates.Queries;
using Application.RobotConfiguration.ArtifactTemplates.Commands;
using Domain.RobotConfiguration.ArtifactTemplates;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Domain.RobotConfiguration.Artifacts;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.RobotConfiguration.ArtifactTemplates.Persistence;

public sealed class RobotArtifactTemplateStore : IRobotArtifactTemplateStore
{
    private readonly IceBotDbContext _dbContext;

    public RobotArtifactTemplateStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RobotArtifactTemplate?> GetByIdAsync(
        Guid templateId,
        bool tracked = false,
        CancellationToken cancellationToken = default)
    {
        var query = tracked
            ? _dbContext.RobotArtifactTemplates
            : _dbContext.RobotArtifactTemplates.AsNoTracking();
        return query.FirstOrDefaultAsync(template => template.Id == templateId, cancellationToken);
    }

    public Task<RobotArtifactTemplate?> GetByCodeAndChecksumAsync(
        string templateCode,
        string checksum,
        CancellationToken cancellationToken = default) =>
        _dbContext.RobotArtifactTemplates.AsNoTracking().FirstOrDefaultAsync(
            template => template.TemplateCode == templateCode &&
                template.Checksum == checksum &&
                template.DeletedAt == null,
            cancellationToken);

    public Task<int> CountAsync(
        string? search,
        RobotArtifactStatus? status,
        CancellationToken cancellationToken = default) =>
        BuildQuery(search, status).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<RobotArtifactTemplate>> ListAsync(
        string? search,
        RobotArtifactStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        await BuildQuery(search, status)
            .OrderBy(template => template.TemplateCode)
            .ThenByDescending(template => template.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<RobotArtifactTemplateInsertResult> InsertOrGetExistingAsync(
        RobotArtifactTemplate template,
        CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.RobotArtifactTemplates.AddAsync(template, cancellationToken);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new RobotArtifactTemplateInsertResult(true, template);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            entry.State = EntityState.Detached;
            var existing = await GetByCodeAndChecksumAsync(template.TemplateCode, template.Checksum, cancellationToken);
            if (existing is null)
            {
                throw;
            }

            return new RobotArtifactTemplateInsertResult(false, existing);
        }
    }

    public async Task<RobotArtifactTemplateDiscardOutcome> DiscardDraftAsync(
        RobotArtifactTemplate template,
        CancellationToken cancellationToken = default)
    {
        var entry = _dbContext.RobotArtifactTemplates.Remove(template);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return RobotArtifactTemplateDiscardOutcome.Deleted;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            entry.State = EntityState.Unchanged;
            return RobotArtifactTemplateDiscardOutcome.Referenced;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<RobotArtifactTemplate> BuildQuery(string? search, RobotArtifactStatus? status)
    {
        var query = _dbContext.RobotArtifactTemplates.AsNoTracking()
            .Where(template => template.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(template =>
                EF.Functions.ILike(template.TemplateCode, $"%{term}%") ||
                EF.Functions.ILike(template.TemplateName, $"%{term}%") ||
                EF.Functions.ILike(template.FileName, $"%{term}%"));
        }

        if (status.HasValue)
        {
            query = query.Where(template => template.Status == status.Value);
        }

        return query;
    }
}
