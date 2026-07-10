using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Programs.Commands;

public sealed class ReplaceRobotProgramArtifactsCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ProgramId { get; init; }
    public IReadOnlyCollection<RobotProgramArtifactInput> Artifacts { get; init; } = Array.Empty<RobotProgramArtifactInput>();
}

public sealed record RobotProgramArtifactInput(
    Guid RobotArtifactId,
    int RunOrder,
    int ParametersSchemaVersion,
    string? ParametersJson);
