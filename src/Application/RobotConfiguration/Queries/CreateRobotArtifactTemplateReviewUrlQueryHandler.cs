using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Queries;

public sealed class CreateRobotArtifactTemplateReviewUrlQueryHandler
{
    private readonly IRobotArtifactTemplateStore _store;
    private readonly IArtifactObjectStorage _storage;

    public CreateRobotArtifactTemplateReviewUrlQueryHandler(
        IRobotArtifactTemplateStore store,
        IArtifactObjectStorage storage)
    {
        _store = store;
        _storage = storage;
    }

    public async Task<ApiResult<RobotArtifactReviewUrlResult>> HandleAsync(
        CreateRobotArtifactTemplateReviewUrlQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!CanReadTemplates(query.UserContext))
        {
            return ApiResult<RobotArtifactReviewUrlResult>.Fail("Access denied.", 403);
        }

        var template = await _store.GetByIdAsync(query.TemplateId, cancellationToken: cancellationToken);
        if (template is null)
        {
            return ApiResult<RobotArtifactReviewUrlResult>.Fail("Robot artifact template not found.", 404);
        }

        var url = await _storage.CreateReadUrlAsync(template.StorageKey, cancellationToken);
        return ApiResult<RobotArtifactReviewUrlResult>.Success(new RobotArtifactReviewUrlResult
        {
            RobotArtifactId = template.Id,
            Url = url.Url,
            ExpiresAt = url.ExpiresAt,
            Checksum = template.Checksum,
            ContentLengthBytes = template.ContentLengthBytes
        });
    }

    private static bool CanReadTemplates(CurrentUserContext userContext) =>
        userContext.IsSystemAdmin || userContext.RoleScopes.Any(
            scope => string.Equals(scope.RoleCode, "OrgAdmin", StringComparison.OrdinalIgnoreCase));
}
