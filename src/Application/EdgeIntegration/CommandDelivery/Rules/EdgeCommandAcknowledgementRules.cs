using Application.EdgeIntegration.CommandDelivery.Commands;

namespace Application.EdgeIntegration.CommandDelivery.Rules;

public static class EdgeCommandAcknowledgementRules
{
    public static string? Validate(AcknowledgeEdgeCommandCommand command)
    {
        if (string.Equals(command.AckStatus.Trim(), "Accepted", StringComparison.OrdinalIgnoreCase) &&
            command.LocalStatePersisted != true)
            return "Accepted acknowledgement requires durable local command state.";
        return null;
    }
}
