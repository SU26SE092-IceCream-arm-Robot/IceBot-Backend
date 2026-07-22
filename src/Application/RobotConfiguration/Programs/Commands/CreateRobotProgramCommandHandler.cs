using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Queries;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Application.RobotConfiguration.Programs.Abstractions;
using Application.RobotConfiguration.Programs.Results;
using Application.Shared.Wrappers;
using Application.Shared.Concurrency;
using Application.Tenants;
using Domain.Common;
using Domain.RobotConfiguration.Artifacts;
using Domain.Tenants.Enums;

namespace Application.RobotConfiguration.Programs.Commands;

public sealed class CreateRobotProgramCommandHandler
{
    private readonly IRobotProgramStore _robotProgramStore;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public CreateRobotProgramCommandHandler(
        IRobotProgramStore robotProgramStore,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _robotProgramStore = robotProgramStore;
        _mutations = mutationCoordinator;
    }

    public async Task<ApiResult<RobotProgramResult>> HandleAsync(
        CreateRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramManage, command.UserContext, command.OrganizationId, command.StoreId, command.KioskId))
        {
            return ApiResult<RobotProgramResult>.Fail("Access denied.", 403);
        }

        var scopeType = TenantScopeResolver.Resolve(command.StoreId, command.KioskId, command.DeviceId);

        if (!await _robotProgramStore.ProgramScopeExistsAsync(
                scopeType,
                command.OrganizationId,
                command.StoreId,
                command.KioskId,
                command.DeviceId,
                cancellationToken))
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program scope does not exist or its ids do not belong to the same tenant hierarchy.", 400);
        }

        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        return await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.ProgramDefinition(
                command.OrganizationId, command.StoreId, command.KioskId, command.DeviceId, normalizedCode)],
            async ct =>
            {
                if (await _robotProgramStore.ProgramCodeExistsAsync(
                        command.OrganizationId,
                        command.StoreId,
                        command.KioskId,
                        command.DeviceId,
                        normalizedCode,
                        cancellationToken: ct))
                {
                    return ApiResult<RobotProgramResult>.Fail(
                        "Robot program code already exists in the selected scope.", 409);
                }

                try
                {
                    var program = RobotProgram.CreateDraft(
                        normalizedCode,
                        command.Name,
                        scopeType,
                        command.OrganizationId,
                        command.StoreId,
                        command.KioskId,
                        command.DeviceId,
                        command.Description);
                    program.CreatedByAccountId = command.UserContext.AccountId;

                    await _robotProgramStore.AddProgramAsync(program, ct);
                    await _robotProgramStore.SaveChangesAsync(ct);

                    return ApiResult<RobotProgramResult>.Success(
                        RobotProgramResult.FromEntity(program),
                        "Robot program draft created successfully.",
                        201);
                }
                catch (DomainRuleException ex)
                {
                    return ApiResult<RobotProgramResult>.Fail(ex.Message, 400);
                }
            },
            cancellationToken);
    }
}
