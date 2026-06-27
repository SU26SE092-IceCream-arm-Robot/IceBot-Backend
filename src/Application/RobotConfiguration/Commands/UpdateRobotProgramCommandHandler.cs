using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;

namespace Application.RobotConfiguration.Commands;

public sealed class UpdateRobotProgramCommandHandler
{
    private readonly IRobotConfigurationStore _store;

    public UpdateRobotProgramCommandHandler(IRobotConfigurationStore store) => _store = store;

    public async Task<ApiResult<RobotProgramResult>> HandleAsync(
        UpdateRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        var program = await _store.GetProgramForEditAsync(command.ProgramId, cancellationToken);
        if (program is null)
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(command.UserContext, program.OrganizationId, program.StoreId, program.KioskId))
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
                RobotProgramResult.FromEntity(program), "Robot program updated successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RobotProgramResult>.Fail(ex.Message, 400);
        }
    }
}
