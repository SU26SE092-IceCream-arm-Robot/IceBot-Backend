using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;

namespace Application.RobotConfiguration.Commands;

public sealed class RetireRobotProgramCommandHandler
{
    private readonly IRobotConfigurationStore _store;
    public RetireRobotProgramCommandHandler(IRobotConfigurationStore store) => _store = store;

    public async Task<ApiResult<RobotProgramResult>> HandleAsync(
        RetireRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        var program = await _store.GetProgramForPublishAsync(command.ProgramId, cancellationToken);
        if (program is null || program.OrganizationId != command.OrganizationId)
            return ApiResult<RobotProgramResult>.Fail("Robot program not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramManage, command.UserContext, program.OrganizationId, program.StoreId, program.KioskId))
            return ApiResult<RobotProgramResult>.Fail("Access denied.", 403);
        if (await _store.ProgramIsReferencedByDraftReleaseAsync(program.Id, cancellationToken))
            return ApiResult<RobotProgramResult>.Fail("Remove the robot program from draft configuration releases before retiring it.", 409);

        try
        {
            program.Retire(DateTimeOffset.UtcNow);
            program.UpdatedByAccountId = command.UserContext.AccountId;
            await _store.SaveChangesAsync(cancellationToken);
            return ApiResult<RobotProgramResult>.Success(RobotProgramResult.FromEntity(program), "Robot program retired successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RobotProgramResult>.Fail(ex.Message, 400);
        }
    }
}
