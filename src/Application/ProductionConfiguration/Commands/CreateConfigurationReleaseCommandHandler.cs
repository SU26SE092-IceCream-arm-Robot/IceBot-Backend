using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;

namespace Application.ProductionConfiguration.Commands;

public sealed class CreateConfigurationReleaseCommandHandler
{
    private readonly IProductionConfigurationStore _store;

    public CreateConfigurationReleaseCommandHandler(IProductionConfigurationStore store) => _store = store;

    public async Task<ApiResult<ConfigurationReleaseResult>> HandleAsync(
        CreateConfigurationReleaseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(command.UserContext, command.OrganizationId, null, null))
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Access denied.", 403);
        }

        if (!await _store.OrganizationExistsAsync(command.OrganizationId, cancellationToken))
        {
            return ApiResult<ConfigurationReleaseResult>.Fail("Organization not found.", 404);
        }

        try
        {
            var release = await _store.CreateNextReleaseAsync(
                command.OrganizationId,
                releaseNumber =>
                {
                    var draft = ConfigurationRelease.CreateDraft(
                        command.OrganizationId,
                        releaseNumber,
                        command.ReleaseManifestSchemaVersion);
                    draft.CreatedByAccountId = command.UserContext.AccountId;
                    return draft;
                },
                cancellationToken);
            return ApiResult<ConfigurationReleaseResult>.Success(
                ConfigurationReleaseResult.FromEntity(release),
                "Configuration release draft created successfully.",
                201);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ConfigurationReleaseResult>.Fail(ex.Message, 400);
        }
    }
}
