using Application.Identity.Tokens.Claims;
using Domain.RobotConfiguration.Enums;

namespace Application.RobotConfiguration.Queries;

public sealed class ListRobotArtifactTemplatesQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public string? Search { get; init; }
    public RobotArtifactStatus? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record GetRobotArtifactTemplateQuery(Guid TemplateId)
{
    public required CurrentUserContext UserContext { get; init; }
}

public sealed record CreateRobotArtifactTemplateReviewUrlQuery(Guid TemplateId)
{
    public required CurrentUserContext UserContext { get; init; }
}
