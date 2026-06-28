using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.RobotConfiguration.Entities;
using Domain.Tenants.Enums;

namespace Application.RobotConfiguration.Commands;

public sealed class CreateRobotProgramCommandHandler
{
    private readonly IRobotConfigurationStore _robotConfigurationStore;

    public CreateRobotProgramCommandHandler(IRobotConfigurationStore robotConfigurationStore)
    {
        _robotConfigurationStore = robotConfigurationStore;
    }

    public async Task<ApiResult<RobotProgramResult>> HandleAsync(
        CreateRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(command.UserContext, command.OrganizationId, command.StoreId, command.KioskId))
        {
            return ApiResult<RobotProgramResult>.Fail("Access denied.", 403);
        }

        if (command.ScopeType == TenantScopeType.Global)
        {
            return ApiResult<RobotProgramResult>.Fail("Global robot programs are not supported because robot artifacts are organization-owned.", 400);
        }

        if (!await _robotConfigurationStore.ProgramScopeExistsAsync(
                command.ScopeType,
                command.OrganizationId,
                command.StoreId,
                command.KioskId,
                command.DeviceId,
                cancellationToken))
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program scope does not exist or its ids do not belong to the same tenant hierarchy.", 400);
        }

        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        if (await _robotConfigurationStore.ProgramCodeExistsAsync(
                command.OrganizationId,
                command.StoreId,
                command.KioskId,
                command.DeviceId,
                normalizedCode,
                cancellationToken: cancellationToken))
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program code already exists in the selected scope.", 409);
        }

        try
        {
            var program = RobotProgram.CreateDraft(
                normalizedCode,
                command.Name,
                command.ScopeType,
                command.OrganizationId,
                command.StoreId,
                command.KioskId,
                command.DeviceId,
                command.Description);
            program.CreatedByAccountId = command.UserContext.AccountId;

            await _robotConfigurationStore.AddProgramAsync(program, cancellationToken);
            await _robotConfigurationStore.SaveChangesAsync(cancellationToken);

            return ApiResult<RobotProgramResult>.Success(
                RobotProgramResult.FromEntity(program),
                "Robot program draft created successfully.",
                201);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RobotProgramResult>.Fail(ex.Message, 400);
        }
    }
}
