using Application.Identity.Tokens.Claims;
using Domain.RobotConfiguration.Enums;

namespace Application.RobotConfiguration.Queries;

public sealed class ListRobotProgramsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public string? Search { get; init; }
    public RobotProgramStatus? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
