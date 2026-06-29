using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Queries;

public sealed class GetRobotArtifactTemplateQueryHandler
{
    private readonly IRobotArtifactTemplateStore _store;

    public GetRobotArtifactTemplateQueryHandler(IRobotArtifactTemplateStore store)
    {
        _store = store;
    }

    public async Task<ApiResult<RobotArtifactTemplateResult>> HandleAsync(
        GetRobotArtifactTemplateQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!CanReadTemplates(query.UserContext))
        {
            return ApiResult<RobotArtifactTemplateResult>.Fail("Access denied.", 403);
        }

        var template = await _store.GetByIdAsync(query.TemplateId, cancellationToken: cancellationToken);
        return template is null
            ? ApiResult<RobotArtifactTemplateResult>.Fail("Robot artifact template not found.", 404)
            : ApiResult<RobotArtifactTemplateResult>.Success(RobotArtifactTemplateResult.FromEntity(template));
    }

    private static bool CanReadTemplates(CurrentUserContext userContext) =>
        userContext.IsSystemAdmin || userContext.RoleScopes.Any(
            scope => string.Equals(scope.RoleCode, "OrgAdmin", StringComparison.OrdinalIgnoreCase));
}
