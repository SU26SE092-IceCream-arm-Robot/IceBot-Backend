using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.RobotConfiguration.Enums;
using Domain.RobotConfiguration.Manifests;

namespace Application.ProductionConfiguration.Commands;

public sealed class PublishConfigurationReleaseCommandHandler
{
    private readonly IProductionConfigurationStore _productionConfigurationStore;

    public PublishConfigurationReleaseCommandHandler(IProductionConfigurationStore productionConfigurationStore)
    {
        _productionConfigurationStore = productionConfigurationStore;
    }

    public async Task<ApiResult<ConfigurationReleaseResult>> HandleAsync(
        PublishConfigurationReleaseCommand command,
        CancellationToken cancellationToken = default)
    {
        var release = await _productionConfigurationStore.GetReleaseForPublishAsync(command.ReleaseId, cancellationToken);
        if (release is null || release.OrganizationId != command.OrganizationId)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Configuration release not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleasePublish, command.UserContext, release.OrganizationId, null, null))
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Access denied.", 403);
        }

        try
        {
            var snapshots = release.ExecutionRoutes
                .SelectMany(route => route.RobotBindings)
                .Select(binding => binding.RobotProgram)
                .DistinctBy(program => program.Id)
                .ToDictionary(
                    program => program.Id,
                    program => CreateSnapshot(program, release.OrganizationId));
            release.Publish(DateTimeOffset.UtcNow, command.UserContext.AccountId, snapshots);
            release.UpdatedByAccountId = command.UserContext.AccountId;
            await _productionConfigurationStore.SaveChangesAsync(cancellationToken);
            return ApiResult<ConfigurationReleaseResult>.Success(
                ConfigurationReleaseResult.FromEntity(release),
                "Configuration release published successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(ex.Message, 400);
        }
    }

    private static PublishedRobotProgramSnapshot CreateSnapshot(
        Domain.RobotConfiguration.Entities.RobotProgram program,
        Guid organizationId)
    {
        if (program.Status != RobotProgramStatus.Published ||
            program.OrganizationId != organizationId ||
            string.IsNullOrWhiteSpace(program.ProgramManifestChecksum) ||
            string.IsNullOrWhiteSpace(program.ProgramManifestJson))
            throw new DomainRuleException("Configuration release requires published organization-owned robot programs.");

        var manifest = RobotProgramManifestBuilder.Parse(program.ProgramManifestJson);
        if (manifest.Id != program.Id || manifest.SchemaVersion != program.ProgramManifestSchemaVersion)
            throw new DomainRuleException("Robot program manifest identity does not match the published program.");

        return new PublishedRobotProgramSnapshot(
            program.Id,
            program.Code,
            organizationId,
            program.ProgramManifestSchemaVersion,
            program.ProgramManifestChecksum,
            manifest.Artifacts.Select(item => new PublishedRobotArtifactSnapshot(
                item.Id,
                item.RobotArtifact.Id,
                item.RunOrder,
                item.ParametersSchemaVersion,
                item.Parameters?.ToJsonString(),
                item.RobotArtifact.Checksum,
                item.RobotArtifact.StorageKey,
                item.RobotArtifact.RuntimeTargetCode,
                item.RobotArtifact.MachineModelCode,
                item.RobotArtifact.ContentLengthBytes)).ToArray());
    }
}
