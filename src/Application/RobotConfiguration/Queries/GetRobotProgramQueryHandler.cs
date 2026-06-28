using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.RobotConfiguration.Queries;

public sealed class GetRobotProgramQueryHandler
{
    private readonly IRobotConfigurationStore _store;

    public GetRobotProgramQueryHandler(IRobotConfigurationStore store) => _store = store;

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

        return ApiResult<RobotProgramResult>.Success(RobotProgramResult.FromEntity(program));
    }
}
