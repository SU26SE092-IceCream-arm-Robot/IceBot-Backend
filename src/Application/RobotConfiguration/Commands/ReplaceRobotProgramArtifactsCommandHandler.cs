using System.Text.Json;
using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;

namespace Application.RobotConfiguration.Commands;

public sealed class ReplaceRobotProgramArtifactsCommandHandler
{
    private readonly IRobotConfigurationStore _robotConfigurationStore;

    public ReplaceRobotProgramArtifactsCommandHandler(IRobotConfigurationStore robotConfigurationStore)
    {
        _robotConfigurationStore = robotConfigurationStore;
    }

    public async Task<ApiResult<RobotProgramResult>> HandleAsync(
        ReplaceRobotProgramArtifactsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Artifacts.Count == 0)
        {
            return ApiResult<RobotProgramResult>.Fail("At least one robot artifact is required.", 400);
        }

        if (command.Artifacts.Any(item => item.RobotArtifactId == Guid.Empty || item.RunOrder <= 0 || item.ParametersSchemaVersion <= 0))
        {
            return ApiResult<RobotProgramResult>.Fail("Artifact id, positive run order, and positive parameters schema version are required.", 400);
        }

        if (command.Artifacts.GroupBy(item => item.RunOrder).Any(group => group.Count() > 1))
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program artifact run orders must be unique.", 400);
        }

        if (command.Artifacts.Any(item => !IsValidOptionalJson(item.ParametersJson)))
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program artifact parameters must be valid JSON.", 400);
        }

        var program = await _robotConfigurationStore.GetProgramForEditAsync(command.ProgramId, cancellationToken);
        if (program is null)
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(command.UserContext, program.OrganizationId, program.StoreId, program.KioskId))
        {
            return ApiResult<RobotProgramResult>.Fail("Access denied.", 403);
        }

        if (!program.OrganizationId.HasValue)
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program must belong to an organization before artifacts can be assigned.", 400);
        }

        var artifactIds = command.Artifacts.Select(item => item.RobotArtifactId).Distinct().ToArray();
        var artifacts = await _robotConfigurationStore.ListArtifactsByIdsAsync(artifactIds, cancellationToken);
        if (artifacts.Count != artifactIds.Length)
        {
            return ApiResult<RobotProgramResult>.Fail("One or more robot artifacts were not found.", 400);
        }

        if (artifacts.Any(artifact => artifact.OrganizationId != program.OrganizationId.Value))
        {
            return ApiResult<RobotProgramResult>.Fail("All robot artifacts must belong to the robot program organization.", 400);
        }

        try
        {
            var existingArtifacts = program.RobotProgramArtifacts.ToArray();
            _robotConfigurationStore.DeleteProgramArtifacts(existingArtifacts);
            program.ClearArtifacts();

            foreach (var item in command.Artifacts.OrderBy(item => item.RunOrder))
            {
                program.AddArtifact(
                    item.RobotArtifactId,
                    item.RunOrder,
                    string.IsNullOrWhiteSpace(item.ParametersJson) ? null : item.ParametersJson.Trim(),
                    item.ParametersSchemaVersion);
            }

            program.UpdatedByAccountId = command.UserContext.AccountId;
            await _robotConfigurationStore.SaveChangesAsync(cancellationToken);

            var updatedProgram = await _robotConfigurationStore.GetProgramByIdAsync(program.Id, cancellationToken)
                ?? throw new InvalidOperationException("Robot program disappeared after artifact replacement.");

            return ApiResult<RobotProgramResult>.Success(
                RobotProgramResult.FromEntity(updatedProgram),
                "Robot program artifact order replaced successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RobotProgramResult>.Fail(ex.Message, 400);
        }
    }

    private static bool IsValidOptionalJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
