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

public sealed class UpdateRobotProgramCommandHandler
{
    private readonly IRobotProgramStore _store;
    private readonly IRobotArtifactStore _artifactStore;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public UpdateRobotProgramCommandHandler(
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
        UpdateRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        var observedProgram = await _store.GetProgramByIdAsync(command.ProgramId, cancellationToken);
        if (observedProgram is null || observedProgram.OrganizationId != command.OrganizationId)
            return ApiResult<RobotProgramResult>.Fail("Robot program not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.ProgramManage, command.UserContext,
                observedProgram.OrganizationId, observedProgram.StoreId, observedProgram.KioskId))
            return ApiResult<RobotProgramResult>.Fail("Access denied.", 403);
        if (!observedProgram.OrganizationId.HasValue)
            return ApiResult<RobotProgramResult>.Fail("Robot program must belong to an organization.", 400);

        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        return await _mutations.ExecuteAsync(
            [
                TechnicalResourceMutationIdentity.Program(command.ProgramId),
                TechnicalResourceMutationIdentity.ProgramDefinition(
                    observedProgram.OrganizationId.Value, observedProgram.StoreId, observedProgram.KioskId,
                    observedProgram.DeviceId, observedProgram.Code),
                TechnicalResourceMutationIdentity.ProgramDefinition(
                    observedProgram.OrganizationId.Value, observedProgram.StoreId, observedProgram.KioskId,
                    observedProgram.DeviceId, normalizedCode)
            ],
            async ct =>
            {
                var program = await _store.GetProgramForEditAsync(command.ProgramId, ct);
                if (program is null || program.OrganizationId != command.OrganizationId)
                    return ApiResult<RobotProgramResult>.Fail("Robot program not found.", 404);
                if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramManage, command.UserContext, program.OrganizationId, program.StoreId, program.KioskId))
                    return ApiResult<RobotProgramResult>.Fail("Access denied.", 403);
                var ownershipError = await _technicalOwnership.ValidateDefinitionMutationAsync(
                    TechnicalResourceKind.RobotProgram, program.Id, ct);
                if (ownershipError is not null) return ApiResult<RobotProgramResult>.Fail(ownershipError, 409);
                if (program.OrganizationId.HasValue && await _store.ProgramCodeExistsAsync(
                        program.OrganizationId.Value, program.StoreId, program.KioskId, program.DeviceId,
                        normalizedCode, program.Id, ct))
                    return ApiResult<RobotProgramResult>.Fail("Robot program code already exists in the selected scope.", 409);
                try
                {
                    program.UpdateDraftDetails(normalizedCode, command.Name, command.Description);
                    program.UpdatedByAccountId = command.UserContext.AccountId;
                    await _store.SaveChangesAsync(ct);
                    return ApiResult<RobotProgramResult>.Success(
                        await RobotProgramResultMapper.ToResultAsync(_artifactStore, program, ct),
                        "Robot program updated successfully.");
                }
                catch (DomainRuleException ex) { return ApiResult<RobotProgramResult>.Fail(ex.Message, 400); }
            }, cancellationToken);
    }
}
