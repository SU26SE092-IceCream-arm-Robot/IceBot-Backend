using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.ProductionConfiguration.Enums;

namespace Application.ProductionConfiguration.Releases.Commands;

public sealed class DiscardDraftConfigurationReleaseCommandHandler
{
    private readonly IConfigurationReleaseStore _store;
    private readonly IConfigurationDeploymentStore _deployments;

    public DiscardDraftConfigurationReleaseCommandHandler(IConfigurationReleaseStore store, IConfigurationDeploymentStore deployments)
    {
        _store = store;
        _deployments = deployments;
    }

    public async Task<ApiResult<object>> HandleAsync(
        DiscardDraftConfigurationReleaseCommand command,
        CancellationToken cancellationToken = default)
    {
        var release = await _store.GetReleaseForEditAsync(command.ReleaseId, cancellationToken);
        if (release is null || release.OrganizationId != command.OrganizationId)
            return ApiResult<object>.Fail("Configuration release not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleasePublish, command.UserContext, release.OrganizationId, null, null))
            return ApiResult<object>.Fail("Access denied.", 403);
        if (release.Status != ConfigurationReleaseStatus.Draft)
            return ApiResult<object>.Fail("Only draft configuration releases can be discarded.", 400);

        if (await _deployments.HasAnyDeploymentsForReleaseAsync(release.Id, cancellationToken))
            return ApiResult<object>.Fail("Configuration release has deployment references and cannot be discarded.", 409);

        var outcome = await _store.DiscardDraftReleaseAsync(release, cancellationToken);
        if (outcome == ConfigurationReleaseDiscardOutcome.Referenced)
            return ApiResult<object>.Fail("Configuration release has deployment references and cannot be discarded.", 409);

        return ApiResult<object>.Success(
            new { ConfigurationReleaseId = command.ReleaseId },
            "Draft configuration release discarded successfully.");
    }
}
