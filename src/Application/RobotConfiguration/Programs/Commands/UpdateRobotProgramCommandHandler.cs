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

public sealed class UpdateRobotProgramCommandHandler
{
    private readonly IRobotProgramStore _store;
    private readonly IRobotArtifactStore _artifactStore;

    public UpdateRobotProgramCommandHandler(IRobotProgramStore store, IRobotArtifactStore artifactStore)
    {
        _store = store;
        _artifactStore = artifactStore;
    }

    public async Task<ApiResult<RobotProgramResult>> HandleAsync(
        UpdateRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        var program = await _store.GetProgramForEditAsync(command.ProgramId, cancellationToken);
        if (program is null || program.OrganizationId != command.OrganizationId)
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramManage, command.UserContext, program.OrganizationId, program.StoreId, program.KioskId))
        {
            return ApiResult<RobotProgramResult>.Fail("Access denied.", 403);
        }

        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        if (program.OrganizationId.HasValue && await _store.ProgramCodeExistsAsync(
                program.OrganizationId.Value, program.StoreId, program.KioskId, program.DeviceId,
                normalizedCode, program.Id, cancellationToken))
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program code already exists in the selected scope.", 409);
        }

        try
        {
            program.UpdateDraftDetails(normalizedCode, command.Name, command.Description);
            program.UpdatedByAccountId = command.UserContext.AccountId;
            await _store.SaveChangesAsync(cancellationToken);
            return ApiResult<RobotProgramResult>.Success(
                await RobotProgramResultMapper.ToResultAsync(_artifactStore, program, cancellationToken),
                "Robot program updated successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RobotProgramResult>.Fail(ex.Message, 400);
        }
    }
}
