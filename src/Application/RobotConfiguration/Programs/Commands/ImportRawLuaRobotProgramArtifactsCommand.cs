using Application.Identity.Tokens.Claims;
using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Programs.Results;

namespace Application.RobotConfiguration.Programs.Commands;

/// <summary>
/// Imports one or more legacy/raw Lua files directly into an existing draft program.
/// Strict Fairino bundles continue to use the authoring-import lifecycle.
/// </summary>
public sealed class ImportRawLuaRobotProgramArtifactsCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid ProgramId { get; init; }
    public required string RuntimeTargetCode { get; init; }
    public required string MachineModelCode { get; init; }
    public string? Description { get; init; }
    public IReadOnlyCollection<RawLuaRobotProgramArtifactInput> Artifacts { get; init; } = [];
}

public sealed class RawLuaRobotProgramArtifactInput
{
    public required string FileName { get; init; }
    public required string ArtifactCode { get; init; }
    public required string ArtifactName { get; init; }
    public required string ContentType { get; init; }
    public required long ContentLengthBytes { get; init; }
    public required Stream Content { get; init; }
}

public sealed class RawLuaRobotProgramArtifactImportResult
{
    public required BulkRobotArtifactUploadResult Upload { get; init; }
    public RobotProgramResult? Program { get; init; }
    public IReadOnlyCollection<Guid> AppendedArtifactIds { get; init; } = [];
    public bool AppendPending => Program is null && Upload.SucceededCount > 0;
}
