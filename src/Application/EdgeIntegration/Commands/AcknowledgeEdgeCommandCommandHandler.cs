using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Enums;
using Domain.Sync.Entities;
using Domain.Sync.Enums;

namespace Application.EdgeIntegration.Commands;

public sealed class AcknowledgeEdgeCommandCommandHandler
{
    private readonly IEdgeCommandStore _edgeCommandStore;

    public AcknowledgeEdgeCommandCommandHandler(IEdgeCommandStore edgeCommandStore)
    {
        _edgeCommandStore = edgeCommandStore;
    }

    public async Task<ApiResult<EdgeCommandAckResult>> HandleAsync(
        AcknowledgeEdgeCommandCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty || command.CommandId == Guid.Empty)
        {
            return ApiResult<EdgeCommandAckResult>.Fail("Kiosk, execution endpoint, and command are required.", 400);
        }

        var endpoint = await _edgeCommandStore.GetEndpointForCommandAuthAsync(command.EndpointId, cancellationToken);
        if (endpoint is null ||
            endpoint.KioskId != command.KioskId ||
            endpoint.Status != KioskExecutionEndpointStatus.Active ||
            endpoint.CredentialBinding is null ||
            endpoint.CredentialBinding.Status != ExecutionEndpointCredentialBindingStatus.Active ||
            !string.Equals(endpoint.CredentialBinding.CredentialReference, command.Credential.Trim(), StringComparison.Ordinal))
        {
            return ApiResult<EdgeCommandAckResult>.Fail("Execution endpoint authentication failed.", 401);
        }

        var edgeCommand = await _edgeCommandStore.GetByIdAsync(command.CommandId, cancellationToken);
        if (edgeCommand is null || edgeCommand.KioskId != command.KioskId || edgeCommand.TargetExecutionEndpointId != command.EndpointId)
        {
            return ApiResult<EdgeCommandAckResult>.Fail("Edge command not found.", 404);
        }

        var observedAt = command.AcknowledgedAt ?? DateTimeOffset.UtcNow;
        try
        {
            ApplyAck(edgeCommand, command, observedAt);
        }
        catch (Domain.Common.DomainRuleException ex)
        {
            return ApiResult<EdgeCommandAckResult>.Fail(ex.Message, 400);
        }

        await _edgeCommandStore.SaveChangesAsync(cancellationToken);

        return ApiResult<EdgeCommandAckResult>.Success(
            EdgeCommandAckResult.FromCommand(edgeCommand),
            "Edge command acknowledgement recorded successfully.");
    }

    private static void ApplyAck(EdgeCommand edgeCommand, AcknowledgeEdgeCommandCommand command, DateTimeOffset observedAt)
    {
        var normalizedStatus = command.AckStatus.Trim();
        if (string.Equals(normalizedStatus, "Received", StringComparison.OrdinalIgnoreCase))
        {
            edgeCommand.RejectIfExpired(observedAt);
            return;
        }

        if (string.Equals(normalizedStatus, "Accepted", StringComparison.OrdinalIgnoreCase))
        {
            if (edgeCommand.Status == EdgeCommandStatus.Accepted)
            {
                return;
            }

            edgeCommand.Accept(observedAt);
            return;
        }

        if (string.Equals(normalizedStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            if (edgeCommand.Status == EdgeCommandStatus.Rejected)
            {
                return;
            }

            edgeCommand.Reject(
                string.IsNullOrWhiteSpace(command.RejectionCode) ? "RejectedByExecutor" : command.RejectionCode,
                command.RejectionMessage,
                observedAt);
            return;
        }

        if (string.Equals(normalizedStatus, "DeliveryFailed", StringComparison.OrdinalIgnoreCase))
        {
            if (edgeCommand.Status == EdgeCommandStatus.DeliveryFailed)
            {
                return;
            }

            var nextAttemptNo = edgeCommand.DeliveryAttempts.Count + 1;
            edgeCommand.RecordDeliveryAttempt(
                nextAttemptNo,
                observedAt,
                EdgeCommandDeliveryOutcome.DeliveryFailed,
                command.RejectionCode,
                command.RejectionMessage);
            return;
        }

        throw new Domain.Common.DomainRuleException("Unsupported dispatch acknowledgement status.");
    }
}
