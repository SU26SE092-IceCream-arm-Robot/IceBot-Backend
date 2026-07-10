using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Queries;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Application.RobotConfiguration.Programs.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.RobotConfiguration.Artifacts;

namespace Application.RobotConfiguration.Programs.Commands;

public sealed class DiscardDraftRobotProgramCommandHandler
{
    private readonly IRobotProgramStore _store;

    public DiscardDraftRobotProgramCommandHandler(IRobotProgramStore store) => _store = store;

    public async Task<ApiResult<object>> HandleAsync(
        DiscardDraftRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        var program = await _store.GetProgramForEditAsync(command.ProgramId, cancellationToken);
        if (program is null || program.OrganizationId != command.OrganizationId)
            return ApiResult<object>.Fail("Robot program not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramManage, command.UserContext, program.OrganizationId, program.StoreId, program.KioskId))
            return ApiResult<object>.Fail("Access denied.", 403);
        if (program.Status != RobotProgramStatus.Draft)
            return ApiResult<object>.Fail("Only draft robot programs can be discarded.", 400);

        var outcome = await _store.DiscardDraftProgramAsync(program, cancellationToken);
        if (outcome == RobotProgramDiscardOutcome.Referenced)
            return ApiResult<object>.Fail("Remove the robot program from configuration releases before discarding it.", 409);

        return ApiResult<object>.Success(new { RobotProgramId = command.ProgramId }, "Draft robot program discarded successfully.");
    }
}
