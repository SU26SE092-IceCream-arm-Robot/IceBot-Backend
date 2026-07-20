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
using Application.Shared.Ownership;
using Application.Shared.Concurrency;

namespace Application.RobotConfiguration.Programs.Commands;

public sealed class RetireRobotProgramCommandHandler
{
    private readonly IRobotProgramStore _store;
    private readonly IRobotArtifactStore _artifactStore;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public RetireRobotProgramCommandHandler(
        IRobotProgramStore store,
        IRobotArtifactStore artifactStore,
        ITechnicalResourceMutationPolicy technicalOwnership,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _store = store;
        _artifactStore = artifactStore;
        _technicalOwnership = technicalOwnership;
        _mutations = mutationCoordinator;
    }

    public async Task<ApiResult<RobotProgramResult>> HandleAsync(
        RetireRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        return await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Program(command.ProgramId)],
            async ct =>
            {
                var program = await _store.GetProgramForPublishAsync(command.ProgramId, ct);
                if (program is null || program.OrganizationId != command.OrganizationId)
                    return ApiResult<RobotProgramResult>.Fail("Robot program not found.", 404);
                if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramManage, command.UserContext, program.OrganizationId, program.StoreId, program.KioskId))
                    return ApiResult<RobotProgramResult>.Fail("Access denied.", 403);
                var ownershipError = await _technicalOwnership.ValidateDefinitionMutationAsync(
                    TechnicalResourceKind.RobotProgram, program.Id, ct);
                if (ownershipError is not null) return ApiResult<RobotProgramResult>.Fail(ownershipError, 409);
                if (await _store.ProgramIsReferencedByDraftReleaseAsync(program.Id, ct))
                    return ApiResult<RobotProgramResult>.Fail("Remove the robot program from draft configuration releases before retiring it.", 409);
                try
                {
                    program.Retire(DateTimeOffset.UtcNow);
                    program.UpdatedByAccountId = command.UserContext.AccountId;
                    await _store.SaveChangesAsync(ct);
                    return ApiResult<RobotProgramResult>.Success(
                        await RobotProgramResultMapper.ToResultAsync(_artifactStore, program, ct),
                        "Robot program retired successfully.");
                }
                catch (DomainRuleException ex) { return ApiResult<RobotProgramResult>.Fail(ex.Message, 400); }
            }, cancellationToken);
    }
}
