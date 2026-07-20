using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Readiness.Services;

namespace Application.ProductionConfiguration.Releases.Commands;

public sealed class PublishConfigurationReleaseCommandHandler
{
    private readonly IConfigurationReleaseStore _releaseStore;
    private readonly ProductionInventoryReadinessGuard _inventoryReadiness;
    private readonly ProductionDefinitionPublicationService _productionDefinitions;

    public PublishConfigurationReleaseCommandHandler(
        IConfigurationReleaseStore releaseStore,
        ProductionInventoryReadinessGuard inventoryReadiness,
        ProductionDefinitionPublicationService productionDefinitions)
    {
        _releaseStore = releaseStore;
        _inventoryReadiness = inventoryReadiness;
        _productionDefinitions = productionDefinitions;
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
            var snapshots = PublishedRobotProgramSnapshotFactory.CreateForPublication(release);
            _productionDefinitions.Build(release, snapshots);
            release.PreparePublication(command.UserContext.AccountId, snapshots);
            var readiness = await _inventoryReadiness.EvaluatePublishAsync(release, cancellationToken);
            if (readiness.IsBlocked)
            {
                return ApiResult<ConfigurationReleaseResult>
                    .Fail("Configuration release inventory readiness policy blocked publication.", 409)
                    .AddDetail("InventoryReadiness", readiness.Results);
            }
            release.Publish(DateTimeOffset.UtcNow, command.UserContext.AccountId, snapshots);
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
    }
}
