using Domain.Devices.ExecutionEndpoints;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.CommandDelivery.Results;
using Application.EdgeIntegration.Dispatch.Results;
using Application.EdgeIntegration.Reports.Results;
using Application.EdgeIntegration.CommandDelivery.Services;
using Application.EdgeIntegration.Dispatch.Services;
using Application.EdgeIntegration.Reports.Services;
using Application.Shared.Wrappers;
using Domain.Devices.Catalog;
using Domain.Sync.Enums;
using Application.EdgeIntegration.Observability;

namespace Application.EdgeIntegration.CommandDelivery.Commands;

public sealed class PullEdgeCommandsCommandHandler
{
    private readonly IEdgeCommandStore _edgeCommandStore;
    private readonly ArtifactCommandPayloadEnricher _artifactPayloadEnricher;

    public PullEdgeCommandsCommandHandler(
        IEdgeCommandStore edgeCommandStore,
        ArtifactCommandPayloadEnricher artifactPayloadEnricher)
    {
        _edgeCommandStore = edgeCommandStore;
        _artifactPayloadEnricher = artifactPayloadEnricher;
    }

    public async Task<ApiResult<EdgeCommandPullResult>> HandleAsync(
        PullEdgeCommandsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty)
        {
            return ApiResult<EdgeCommandPullResult>.Fail("Kiosk and execution endpoint are required.", 400);
        }

        var endpoint = await _edgeCommandStore.GetEndpointForCommandAuthAsync(command.EndpointId, cancellationToken);
        if (endpoint is null ||
            endpoint.KioskId != command.KioskId ||
            endpoint.Status != KioskExecutionEndpointStatus.Active ||
            endpoint.CredentialBinding is null ||
            endpoint.CredentialBinding.Status != ExecutionEndpointCredentialBindingStatus.Active)
        {
            return ApiResult<EdgeCommandPullResult>.Fail("Execution endpoint authentication failed.", 401);
        }

        var now = DateTimeOffset.UtcNow;
        var maxCommands = Math.Clamp(command.MaxCommands, 1, 20);
        var commands = await _edgeCommandStore.ListDispatchableAsync(
            command.KioskId,
            command.EndpointId,
            maxCommands,
            now,
            cancellationToken);

        var commandResults = new List<(Domain.Sync.Entities.EdgeCommand Command, string PayloadJson)>(commands.Count);
        foreach (var edgeCommand in commands)
        {
            var nextAttemptNo = edgeCommand.DeliveryAttempts.Count + 1;
            try
            {
                var payloadJson = await _artifactPayloadEnricher.EnrichAsync(edgeCommand, cancellationToken);
                commandResults.Add((edgeCommand, payloadJson));
                edgeCommand.RecordDeliveryAttempt(nextAttemptNo, now, EdgeCommandDeliveryOutcome.Sent);
            }
            catch (InvalidArtifactCommandPayloadException exception)
            {
                edgeCommand.RecordDeliveryAttempt(
                    nextAttemptNo,
                    now,
                    EdgeCommandDeliveryOutcome.DeliveryFailed,
                    "InvalidDurablePayload",
                    exception.Message);
            }
        }

        await _edgeCommandStore.SaveChangesAsync(cancellationToken);
        foreach (var edgeCommand in commandResults.Select(result => result.Command))
        {
            IceBotEdgeMetrics.RecordCommandPull(now - edgeCommand.CreatedAt, edgeCommand.CommandType.ToString());
        }

        return ApiResult<EdgeCommandPullResult>.Success(
            EdgeCommandPullResult.FromCommands(now, commandResults),
            "Edge commands retrieved successfully.");
    }
}
