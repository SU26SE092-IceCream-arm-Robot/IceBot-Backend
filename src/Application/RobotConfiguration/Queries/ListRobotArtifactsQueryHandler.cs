using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.RobotConfiguration.Queries;

public sealed class ListRobotArtifactsQueryHandler
{
    private readonly IRobotConfigurationStore _store;

    public ListRobotArtifactsQueryHandler(IRobotConfigurationStore store) => _store = store;

    public async Task<PagedResult<RobotArtifactResult>> HandleAsync(
        ListRobotArtifactsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        if (!ScopeAccessRules.CanAccessScopedRow(query.UserContext, query.OrganizationId, null, null))
        {
            return PagedResult<RobotArtifactResult>.Forbidden("Access denied.", pageNumber, pageSize);
        }

        var count = await _store.CountArtifactsAsync(query.OrganizationId, query.Search, query.Status, cancellationToken);
        var artifacts = await _store.ListArtifactsAsync(query.OrganizationId, query.Search, query.Status, pageNumber, pageSize, cancellationToken);
        return PagedResult<RobotArtifactResult>.Success(
            artifacts.Select(RobotArtifactResult.FromEntity), count, pageNumber, pageSize);
    }
}
