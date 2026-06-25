using Domain.RobotConfiguration.Entities;

namespace Application.RobotConfiguration.Results;

public sealed class RobotProgramResult
{
    public Guid Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string ScopeType { get; init; } = null!;
    public string Status { get; init; } = null!;
    public int ProgramManifestSchemaVersion { get; init; }
    public string? ProgramManifestChecksum { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }

    public static RobotProgramResult FromEntity(RobotProgram program)
    {
        return new RobotProgramResult
        {
            Id = program.Id,
            OrganizationId = program.OrganizationId,
            StoreId = program.StoreId,
            KioskId = program.KioskId,
            DeviceId = program.DeviceId,
            Code = program.Code,
            Name = program.Name,
            ScopeType = program.ScopeType.ToString(),
            Status = program.Status.ToString(),
            ProgramManifestSchemaVersion = program.ProgramManifestSchemaVersion,
            ProgramManifestChecksum = program.ProgramManifestChecksum,
            PublishedAt = program.PublishedAt
        };
    }
}
