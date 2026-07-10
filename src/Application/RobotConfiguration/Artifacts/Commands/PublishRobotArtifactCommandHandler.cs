using Application.RobotConfiguration.Artifacts.Queries;
using Domain.RobotConfiguration.Artifacts;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;

namespace Application.RobotConfiguration.Artifacts.Commands;

public sealed class PublishRobotArtifactCommandHandler
{
    private readonly IRobotArtifactStore _robotArtifactStore;

    public PublishRobotArtifactCommandHandler(IRobotArtifactStore robotArtifactStore)
    {
        _robotArtifactStore = robotArtifactStore;
    }

    public async Task<ApiResult<RobotArtifactResult>> HandleAsync(
        PublishRobotArtifactCommand command,
        CancellationToken cancellationToken = default)
    {
        var artifact = await _robotArtifactStore.GetArtifactForPublishAsync(
            command.OrganizationId, command.ArtifactId, cancellationToken);
        if (artifact is null)
        {
            return ApiResult<RobotArtifactResult>.Fail("Robot artifact not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, command.UserContext, command.OrganizationId, null, null))
        {
            return ApiResult<RobotArtifactResult>.Fail("Access denied.", 403);
        }

        try
        {
            artifact.Publish();
            artifact.UpdatedByAccountId = command.UserContext.AccountId;
            await _robotArtifactStore.SaveChangesAsync(cancellationToken);
            return ApiResult<RobotArtifactResult>.Success(RobotArtifactResult.FromEntity(artifact), "Robot artifact published successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RobotArtifactResult>.Fail(ex.Message, 400);
        }
    }
}
