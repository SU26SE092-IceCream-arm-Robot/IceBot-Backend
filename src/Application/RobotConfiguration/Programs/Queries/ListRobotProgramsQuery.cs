using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Application.Identity.Tokens.Claims;
using Domain.RobotConfiguration.Artifacts;

namespace Application.RobotConfiguration.Programs.Queries;

public sealed class ListRobotProgramsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public string? Search { get; init; }
    public RobotProgramStatus? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
