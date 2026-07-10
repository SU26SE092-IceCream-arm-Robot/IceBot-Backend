using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Queries;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Programs.Abstractions;
using Application.RobotConfiguration.Programs.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Application.RobotConfiguration.Programs.Mapping;

namespace Application.RobotConfiguration.Programs.Commands;

public sealed class RetireRobotProgramCommandHandler
{
    private readonly IRobotProgramStore _store;
    private readonly IRobotArtifactStore _artifactStore;

    public RetireRobotProgramCommandHandler(IRobotProgramStore store, IRobotArtifactStore artifactStore)
    {
        _store = store;
        _artifactStore = artifactStore;
    }

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
            return ApiResult<RobotProgramResult>.Success(
                await RobotProgramResultMapper.ToResultAsync(_artifactStore, program, cancellationToken),
                "Robot program retired successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RobotProgramResult>.Fail(ex.Message, 400);
        }
    }
}
