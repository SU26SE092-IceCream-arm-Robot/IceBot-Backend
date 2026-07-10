using Application.RobotConfiguration.Artifacts.Commands;
using Domain.RobotConfiguration.Artifacts;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.RobotConfiguration.Artifacts.Queries;

public sealed class GetRobotArtifactQueryHandler
{
    private readonly IRobotArtifactStore _store;

    public GetRobotArtifactQueryHandler(IRobotArtifactStore store) => _store = store;

    public async Task<ApiResult<RobotArtifactResult>> HandleAsync(
        GetRobotArtifactQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactRead, query.UserContext, query.OrganizationId, null, null))
        {
            return ApiResult<RobotArtifactResult>.Fail("Robot artifact not found.", 404);
        }

        var artifact = await _store.GetArtifactByIdAsync(
            query.OrganizationId,
            query.ArtifactId,
            cancellationToken);
        if (artifact is null)
        {
            return ApiResult<RobotArtifactResult>.Fail("Robot artifact not found.", 404);
        }

        return ApiResult<RobotArtifactResult>.Success(RobotArtifactResult.FromEntity(artifact));
    }
}
