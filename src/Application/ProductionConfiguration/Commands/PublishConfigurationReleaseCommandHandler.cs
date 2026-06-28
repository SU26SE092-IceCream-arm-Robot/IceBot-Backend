using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;

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
        if (release is null)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Configuration release not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(command.UserContext, release.OrganizationId, null, null))
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Access denied.", 403);
        }

        try
        {
            release.Publish(DateTimeOffset.UtcNow, command.UserContext.AccountId);
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
}
