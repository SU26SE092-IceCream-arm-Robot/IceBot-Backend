using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.RobotConfiguration.Artifacts.Queries;

public sealed class GetRobotArtifactUsageQueryHandler(IRobotArtifactUsageReader reader)
{
    public async Task<ApiResult<RobotArtifactUsageResult>> HandleAsync(
        GetRobotArtifactUsageQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.ArtifactRead,
                query.UserContext,
                query.OrganizationId,
                null,
                null))
        {
            return ApiResult<RobotArtifactUsageResult>.Fail("Access denied.", 403);
        }

        var result = await reader.GetAsync(query.OrganizationId, query.ArtifactId, cancellationToken);
        return result is null
            ? ApiResult<RobotArtifactUsageResult>.Fail("Robot artifact not found.", 404)
            : ApiResult<RobotArtifactUsageResult>.Success(result);
    }
}
