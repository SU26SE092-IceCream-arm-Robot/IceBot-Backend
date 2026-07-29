using Application.RobotConfiguration.ArtifactTemplates.Commands;
using Domain.RobotConfiguration.ArtifactTemplates;
using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.Shared.Wrappers;
using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.ArtifactTemplates.Queries;

public sealed class ListRobotArtifactTemplatesQueryHandler
{
    private readonly IRobotArtifactTemplateStore _store;

    public ListRobotArtifactTemplatesQueryHandler(IRobotArtifactTemplateStore store)
    {
        _store = store;
    }

    public async Task<PagedResult<RobotArtifactTemplateResult>> HandleAsync(
        ListRobotArtifactTemplatesQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        if (!CanReadTemplates(query.UserContext))
        {
            return PagedResult<RobotArtifactTemplateResult>.Fail("Access denied.", 403, page, pageSize);
        }

        var count = await _store.CountAsync(query.Search, query.Status, cancellationToken);
        var templates = await _store.ListAsync(query.Search, query.Status, page, pageSize, cancellationToken);
        return PagedResult<RobotArtifactTemplateResult>.Success(
            templates.Select(RobotArtifactTemplateResult.FromEntity),
            count,
            page,
            pageSize);
    }

    private static bool CanReadTemplates(CurrentUserContext userContext) =>
        userContext.IsSystemAdmin || userContext.RoleScopes.Any(
            scope => string.Equals(scope.RoleCode, "OrgAdmin", StringComparison.OrdinalIgnoreCase));
}
