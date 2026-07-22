using Application.RobotConfiguration.Artifacts.Commands;
using Domain.RobotConfiguration.Artifacts;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.RobotConfiguration.Artifacts.Queries;

public sealed class ListRobotArtifactsQueryHandler
{
    private readonly IRobotArtifactStore _store;

    public ListRobotArtifactsQueryHandler(IRobotArtifactStore store) => _store = store;

    public async Task<PagedResult<RobotArtifactResult>> HandleAsync(
        ListRobotArtifactsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactRead, query.UserContext, query.OrganizationId, null, null))
        {
            return PagedResult<RobotArtifactResult>.Forbidden("Access denied.", pageNumber, pageSize);
        }

        var count = await _store.CountArtifactsAsync(query.OrganizationId, query.Search, query.Status, cancellationToken);
        var artifacts = await _store.ListArtifactsAsync(query.OrganizationId, query.Search, query.Status, pageNumber, pageSize, cancellationToken);
        return PagedResult<RobotArtifactResult>.Success(
            artifacts.Select(RobotArtifactResult.FromEntity), count, pageNumber, pageSize);
    }
}
