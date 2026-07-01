using Domain.Devices.ExecutionEndpoints;
using Domain.Common;
using Domain.Sync.Enums;

namespace Domain.Sync.Entities;

public class EdgeCommand : AuditedEntity
{
    private readonly List<EdgeCommandDeliveryAttempt> _deliveryAttempts = [];

    public EdgeCommandType CommandType { get; private set; }
    public int? DispatchAttemptNo { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid KioskId { get; private set; }
    public Guid TargetExecutionEndpointId { get; private set; }
    public Guid? DeploymentId { get; private set; }
    public DeploymentCommandTargetKind? DeploymentKind { get; private set; }
    public string PayloadJson { get; private set; } = null!;
    public DateTimeOffset? CommandExpiryAt { get; private set; }
    public EdgeCommandStatus Status { get; private set; } = EdgeCommandStatus.PendingDelivery;
    public string? RejectionCode { get; private set; }
    public string? RejectionMessage { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public IReadOnlyCollection<EdgeCommandDeliveryAttempt> DeliveryAttempts => _deliveryAttempts;

    public virtual Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint TargetExecutionEndpoint { get; private set; } = null!;

    private EdgeCommand()
    {
    }

    public static EdgeCommand Create(
        EdgeCommandType type,
        Guid kioskId,
        Guid targetEndpointId,
        string payloadJson,
        DateTimeOffset createdAt,
        Guid? orderId = null,
        int? dispatchAttemptNo = null,
        DateTimeOffset? commandExpiryAt = null,
        Guid? deploymentId = null,
        DeploymentCommandTargetKind? deploymentKind = null)
    {
        if (kioskId == Guid.Empty || targetEndpointId == Guid.Empty || string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new DomainRuleException("Kiosk, target execution endpoint, and payload are required.");
        }

        if (type == EdgeCommandType.ExecuteOrder &&
            (orderId is null || dispatchAttemptNo is null || dispatchAttemptNo <= 0 || commandExpiryAt is null))
        {
            throw new DomainRuleException("Execute-order commands require order, dispatch attempt, and expiry.");
        }

        if (type == EdgeCommandType.DeployConfiguration &&
            (!deploymentId.HasValue || deploymentId.Value == Guid.Empty || !deploymentKind.HasValue))
        {
            throw new DomainRuleException("Deploy-configuration commands require a typed deployment target.");
        }

        if (type != EdgeCommandType.DeployConfiguration && (deploymentId.HasValue || deploymentKind.HasValue))
        {
            throw new DomainRuleException("Only deploy-configuration commands can reference a deployment target.");
        }

        if (commandExpiryAt.HasValue && commandExpiryAt.Value <= createdAt)
        {
            throw new DomainRuleException("Edge command expiry must be later than command creation.");
        }

        return new EdgeCommand
        {
            CommandType = type,
            KioskId = kioskId,
            TargetExecutionEndpointId = targetEndpointId,
            DeploymentId = deploymentId,
            DeploymentKind = deploymentKind,
            PayloadJson = payloadJson,
            OrderId = orderId,
            DispatchAttemptNo = dispatchAttemptNo,
            CommandExpiryAt = commandExpiryAt,
            CreatedAt = createdAt
        };
    }

    public EdgeCommandDeliveryAttempt? RecordDeliveryAttempt(
        int deliveryAttemptNo,
        DateTimeOffset sentAt,
        EdgeCommandDeliveryOutcome outcome,
        string? responseCode = null,
        string? responseMessage = null)
    {
        if (Status is EdgeCommandStatus.Accepted or EdgeCommandStatus.Rejected or EdgeCommandStatus.DeliveryFailed)
        {
            throw new DomainRuleException("Cannot record delivery for a terminal command.");
        }

        if (deliveryAttemptNo <= 0 || _deliveryAttempts.Any(attempt => attempt.DeliveryAttemptNo == deliveryAttemptNo))
        {
            throw new DomainRuleException("Delivery attempt number must be unique and greater than zero.");
        }

        if (RejectIfExpired(sentAt))
        {
            return null;
        }

        var attempt = EdgeCommandDeliveryAttempt.Create(
            Id,
            deliveryAttemptNo,
            sentAt,
            outcome,
            responseCode,
            responseMessage);

        _deliveryAttempts.Add(attempt);

        if (outcome == EdgeCommandDeliveryOutcome.Sent)
        {
            Status = EdgeCommandStatus.Delivered;
            DeliveredAt = sentAt;
        }
        else if (outcome is EdgeCommandDeliveryOutcome.ExecutorBusy or EdgeCommandDeliveryOutcome.TransportFailure)
        {
            Status = EdgeCommandStatus.PendingDelivery;
            DeliveredAt = null;
        }
        else if (outcome == EdgeCommandDeliveryOutcome.DeliveryFailed)
        {
            Status = EdgeCommandStatus.DeliveryFailed;
        }

        return attempt;
    }

    public void Accept(DateTimeOffset respondedAt)
    {
        if (Status != EdgeCommandStatus.Delivered)
        {
            throw new DomainRuleException("Only delivered commands can be accepted.");
        }

        if (RejectIfExpired(respondedAt))
        {
            return;
        }

        Status = EdgeCommandStatus.Accepted;
        RespondedAt = respondedAt;
    }

    public bool RejectIfExpired(DateTimeOffset observedAt)
    {
        if (!CommandExpiryAt.HasValue || observedAt <= CommandExpiryAt.Value ||
            Status is EdgeCommandStatus.Accepted or EdgeCommandStatus.Rejected or EdgeCommandStatus.DeliveryFailed)
        {
            return false;
        }

        Status = EdgeCommandStatus.Rejected;
        RejectionCode = "CommandExpired";
        RejectionMessage = "The command expired before the executor accepted it.";
        RespondedAt = observedAt;
        return true;
    }

    public void Reject(string code, string? message, DateTimeOffset respondedAt)
    {
        if (Status != EdgeCommandStatus.Delivered || string.IsNullOrWhiteSpace(code))
        {
            throw new DomainRuleException("Only delivered commands with a rejection code can be rejected.");
        }

        if (RejectIfExpired(respondedAt))
        {
            return;
        }

        Status = EdgeCommandStatus.Rejected;
        RejectionCode = code.Trim();
        RejectionMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        RespondedAt = respondedAt;
    }
}
