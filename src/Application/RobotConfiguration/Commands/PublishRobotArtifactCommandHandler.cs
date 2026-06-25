using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;

namespace Application.RobotConfiguration.Commands;

public sealed class PublishRobotArtifactCommandHandler
{
    private readonly IRobotConfigurationStore _robotConfigurationStore;

    public PublishRobotArtifactCommandHandler(IRobotConfigurationStore robotConfigurationStore)
    {
        _robotConfigurationStore = robotConfigurationStore;
    }

    public async Task<ApiResult<RobotArtifactResult>> HandleAsync(
        PublishRobotArtifactCommand command,
        CancellationToken cancellationToken = default)
    {
        var artifact = await _robotConfigurationStore.GetArtifactForPublishAsync(command.ArtifactId, cancellationToken);
        if (artifact is null)
        {
            return ApiResult<RobotArtifactResult>.Fail("Robot artifact not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(command.UserContext, artifact.OrganizationId, null, null))
        {
            return ApiResult<RobotArtifactResult>.Fail("Access denied.", 403);
        }

        try
        {
            artifact.Publish();
            artifact.UpdatedByAccountId = command.UserContext.AccountId;
            await _robotConfigurationStore.SaveChangesAsync(cancellationToken);
            return ApiResult<RobotArtifactResult>.Success(RobotArtifactResult.FromEntity(artifact), "Robot artifact published successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RobotArtifactResult>.Fail(ex.Message, 400);
        }
    }
}
