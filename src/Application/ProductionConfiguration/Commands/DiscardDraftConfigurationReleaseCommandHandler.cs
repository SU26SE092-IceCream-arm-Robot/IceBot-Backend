using Application.ProductionConfiguration.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.ProductionConfiguration.Enums;

namespace Application.ProductionConfiguration.Commands;

public sealed class DiscardDraftConfigurationReleaseCommandHandler
{
    private readonly IProductionConfigurationStore _store;

    public DiscardDraftConfigurationReleaseCommandHandler(IProductionConfigurationStore store) => _store = store;

    public async Task<ApiResult<object>> HandleAsync(
        DiscardDraftConfigurationReleaseCommand command,
        CancellationToken cancellationToken = default)
    {
        var release = await _store.GetReleaseForEditAsync(command.ReleaseId, cancellationToken);
        if (release is null)
            return ApiResult<object>.Fail("Configuration release not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(command.UserContext, release.OrganizationId, null, null))
            return ApiResult<object>.Fail("Access denied.", 403);
        if (release.Status != ConfigurationReleaseStatus.Draft)
            return ApiResult<object>.Fail("Only draft configuration releases can be discarded.", 400);

        var outcome = await _store.DiscardDraftReleaseAsync(release, cancellationToken);
        if (outcome == ConfigurationReleaseDiscardOutcome.Referenced)
            return ApiResult<object>.Fail("Configuration release has deployment references and cannot be discarded.", 409);

        return ApiResult<object>.Success(
            new { ConfigurationReleaseId = command.ReleaseId },
            "Draft configuration release discarded successfully.");
    }
}
