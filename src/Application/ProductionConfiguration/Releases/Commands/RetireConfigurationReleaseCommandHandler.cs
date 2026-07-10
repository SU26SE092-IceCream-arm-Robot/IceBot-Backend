using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Releases.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;

namespace Application.ProductionConfiguration.Releases.Commands;

public sealed class RetireConfigurationReleaseCommandHandler
{
    private readonly IConfigurationReleaseStore _store;
    private readonly IConfigurationDeploymentStore _deployments;
    public RetireConfigurationReleaseCommandHandler(IConfigurationReleaseStore store, IConfigurationDeploymentStore deployments)
    {
        _store = store;
        _deployments = deployments;
    }

    public async Task<ApiResult<ConfigurationReleaseResult>> HandleAsync(
        RetireConfigurationReleaseCommand command,
        CancellationToken cancellationToken = default)
    {
        var release = await _store.GetReleaseForPublishAsync(command.ReleaseId, cancellationToken);
        if (release is null || release.OrganizationId != command.OrganizationId)
            return ApiResult<ConfigurationReleaseResult>.Fail("Configuration release not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleasePublish, command.UserContext, release.OrganizationId, null, null))
            return ApiResult<ConfigurationReleaseResult>.Fail("Access denied.", 403);
        if (await _deployments.HasPendingDeploymentsForReleaseAsync(release.Id, cancellationToken))
            return ApiResult<ConfigurationReleaseResult>.Fail("Wait for pending or installed deployments to finish before retiring the release.", 409);

        try
        {
            release.Retire(DateTimeOffset.UtcNow);
            release.UpdatedByAccountId = command.UserContext.AccountId;
            await _store.SaveChangesAsync(cancellationToken);
            return ApiResult<ConfigurationReleaseResult>.Success(ConfigurationReleaseResult.FromEntity(release), "Configuration release retired successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(ex.Message, 400);
        }
    }
}
