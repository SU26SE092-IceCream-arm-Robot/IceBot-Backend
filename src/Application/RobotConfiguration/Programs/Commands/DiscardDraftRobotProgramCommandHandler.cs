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
using Application.Shared.Ownership;
using Application.Shared.Concurrency;

namespace Application.RobotConfiguration.Programs.Commands;

public sealed class DiscardDraftRobotProgramCommandHandler
{
    private readonly IRobotProgramStore _store;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public DiscardDraftRobotProgramCommandHandler(
        IRobotProgramStore store,
        ITechnicalResourceMutationPolicy technicalOwnership,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _store = store;
        _technicalOwnership = technicalOwnership;
        _mutations = mutationCoordinator;
    }

    public async Task<ApiResult<object>> HandleAsync(
        DiscardDraftRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        return await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Program(command.ProgramId)],
            async ct =>
            {
                var program = await _store.GetProgramForEditAsync(command.ProgramId, ct);
                if (program is null || program.OrganizationId != command.OrganizationId)
                    return ApiResult<object>.Fail("Robot program not found.", 404);
                if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramManage, command.UserContext, program.OrganizationId, program.StoreId, program.KioskId))
                    return ApiResult<object>.Fail("Access denied.", 403);
                if (program.Status != RobotProgramStatus.Draft)
                    return ApiResult<object>.Fail("Only draft robot programs can be discarded.", 400);
                var ownershipError = await _technicalOwnership.ValidateDefinitionMutationAsync(
                    TechnicalResourceKind.RobotProgram, program.Id, ct);
                if (ownershipError is not null) return ApiResult<object>.Fail(ownershipError, 409);
                var outcome = await _store.DiscardDraftProgramAsync(program, ct);
                if (outcome == RobotProgramDiscardOutcome.Referenced)
                    return ApiResult<object>.Fail("Remove the robot program from configuration releases before discarding it.", 409);
                return ApiResult<object>.Success(new { RobotProgramId = command.ProgramId }, "Draft robot program discarded successfully.");
            }, cancellationToken);
    }
}
