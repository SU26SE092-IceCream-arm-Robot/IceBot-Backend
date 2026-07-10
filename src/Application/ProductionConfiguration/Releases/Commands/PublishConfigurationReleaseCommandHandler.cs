using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.Results;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs.Manifests;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Readiness.Services;

namespace Application.ProductionConfiguration.Releases.Commands;

public sealed class PublishConfigurationReleaseCommandHandler
{
    private readonly IConfigurationReleaseStore _releaseStore;
    private readonly FullEdgeReleaseBundleService _bundleService;
    private readonly ProductionInventoryReadinessGuard _inventoryReadiness;

    public PublishConfigurationReleaseCommandHandler(
        IConfigurationReleaseStore releaseStore,
        FullEdgeReleaseBundleService bundleService,
        ProductionInventoryReadinessGuard inventoryReadiness)
    {
        _releaseStore = releaseStore;
        _bundleService = bundleService;
        _inventoryReadiness = inventoryReadiness;
    }

    public async Task<ApiResult<ConfigurationReleaseResult>> HandleAsync(
        PublishConfigurationReleaseCommand command,
        CancellationToken cancellationToken = default)
    {
        var release = await _releaseStore.GetReleaseForPublishAsync(command.ReleaseId, cancellationToken);
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
            var contentManifest = release.PreparePublication(command.UserContext.AccountId, snapshots);
            var readiness = await _inventoryReadiness.EvaluatePublishAsync(release, cancellationToken);
            if (readiness.IsBlocked)
            {
                return ApiResult<ConfigurationReleaseResult>
                    .Fail("Configuration release inventory readiness policy blocked publication.", 409)
                    .AddDetail("InventoryReadiness", readiness.Results);
            }
            var bundle = await _bundleService.BuildAndStoreAsync(
                release,
                snapshots,
                contentManifest.Json,
                cancellationToken);
            release.Publish(DateTimeOffset.UtcNow, command.UserContext.AccountId, snapshots, bundle);
            release.UpdatedByAccountId = command.UserContext.AccountId;
        await _releaseStore.SaveChangesAsync(cancellationToken);
            var result = ApiResult<ConfigurationReleaseResult>.Success(
                ConfigurationReleaseResult.FromEntity(release),
                "Configuration release published successfully.");
            if (readiness.HasWarnings)
            {
                result.AddDetail("InventoryReadinessWarnings", readiness.Results.Where(item => !item.IsReady).ToArray());
            }
            return result;
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(ex.Message, 400);
        }
        catch (ArtifactObjectNotFoundException ex)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(ex.Message, 409);
        }
        catch (ArtifactObjectIntegrityException ex)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(ex.Message, 409);
        }
        catch (ArtifactObjectSizeLimitExceededException ex)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(ex.Message, 409);
        }
        catch (ArtifactObjectStorageUnavailableException)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(
                "Artifact object storage is temporarily unavailable.",
                503);
        }
    }

    private static PublishedRobotProgramSnapshot CreateSnapshot(
        Domain.RobotConfiguration.Programs.RobotProgram program,
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
