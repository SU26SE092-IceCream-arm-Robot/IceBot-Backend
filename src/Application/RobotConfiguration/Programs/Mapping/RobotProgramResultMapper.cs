using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Programs.Results;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs.Manifests;

namespace Application.RobotConfiguration.Programs.Mapping;

public static class RobotProgramResultMapper
{
    public static Task<IReadOnlyList<RobotArtifactManifestSnapshot>> LoadArtifactSnapshotsAsync(
        IRobotArtifactStore store,
        RobotProgram program,
        CancellationToken cancellationToken = default)
    {
        if (!program.OrganizationId.HasValue)
        {
            return Task.FromResult<IReadOnlyList<RobotArtifactManifestSnapshot>>([]);
        }

        return store.ListArtifactManifestSnapshotsAsync(
            program.OrganizationId.Value,
            program.RobotProgramArtifacts.Select(item => item.RobotArtifactId).Distinct().ToArray(),
            cancellationToken);
    }

    public static async Task<RobotProgramResult> ToResultAsync(
        IRobotArtifactStore store,
        RobotProgram program,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await LoadArtifactSnapshotsAsync(store, program, cancellationToken);
        return RobotProgramResult.FromEntity(program, snapshots);
    }
}
