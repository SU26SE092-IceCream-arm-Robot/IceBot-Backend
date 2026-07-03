using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Domain.RobotConfiguration.Entities;
using Domain.RobotConfiguration.Manifests;

namespace Application.RobotConfiguration.Mapping;

public static class RobotProgramResultMapper
{
    public static Task<IReadOnlyList<RobotArtifactManifestSnapshot>> LoadArtifactSnapshotsAsync(
        IRobotConfigurationStore store,
        RobotProgram program,
        CancellationToken cancellationToken = default)
    {
        return store.ListArtifactManifestSnapshotsAsync(
            program.RobotProgramArtifacts.Select(item => item.RobotArtifactId).Distinct().ToArray(),
            cancellationToken);
    }

    public static async Task<RobotProgramResult> ToResultAsync(
        IRobotConfigurationStore store,
        RobotProgram program,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await LoadArtifactSnapshotsAsync(store, program, cancellationToken);
        return RobotProgramResult.FromEntity(program, snapshots);
    }
}
