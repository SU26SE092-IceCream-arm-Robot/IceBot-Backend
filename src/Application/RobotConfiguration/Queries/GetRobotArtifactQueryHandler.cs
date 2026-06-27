using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.RobotConfiguration.Queries;

public sealed class GetRobotArtifactQueryHandler
{
    private readonly IRobotConfigurationStore _store;

    public GetRobotArtifactQueryHandler(IRobotConfigurationStore store) => _store = store;

    public async Task<ApiResult<RobotArtifactResult>> HandleAsync(
        GetRobotArtifactQuery query,
        CancellationToken cancellationToken = default)
    {
        var artifact = await _store.GetArtifactByIdAsync(query.ArtifactId, cancellationToken);
        if (artifact is null)
        {
            return ApiResult<RobotArtifactResult>.Fail("Robot artifact not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(query.UserContext, artifact.OrganizationId, null, null))
        {
            return ApiResult<RobotArtifactResult>.Fail("Access denied.", 403);
        }

        return ApiResult<RobotArtifactResult>.Success(RobotArtifactResult.FromEntity(artifact));
    }
}
