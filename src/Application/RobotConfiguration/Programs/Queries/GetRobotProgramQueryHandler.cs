using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Programs.Abstractions;
using Application.RobotConfiguration.Programs.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Application.RobotConfiguration.Programs.Mapping;

namespace Application.RobotConfiguration.Programs.Queries;

public sealed class GetRobotProgramQueryHandler
{
    private readonly IRobotProgramStore _store;
    private readonly IRobotArtifactStore _artifactStore;

    public GetRobotProgramQueryHandler(IRobotProgramStore store, IRobotArtifactStore artifactStore)
    {
        _store = store;
        _artifactStore = artifactStore;
    }

    public async Task<ApiResult<RobotProgramResult>> HandleAsync(
        GetRobotProgramQuery query,
        CancellationToken cancellationToken = default)
    {
        var program = await _store.GetProgramByIdAsync(query.ProgramId, cancellationToken);
        if (program is null || program.OrganizationId != query.OrganizationId)
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramRead, query.UserContext, program.OrganizationId, program.StoreId, program.KioskId))
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program not found.", 404);
        }

        return ApiResult<RobotProgramResult>.Success(
            await RobotProgramResultMapper.ToResultAsync(_artifactStore, program, cancellationToken));
    }
}
