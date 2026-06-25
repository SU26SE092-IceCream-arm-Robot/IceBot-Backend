using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Results;

public sealed class EdgeCommandAckResult
{
    public Guid CommandId { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset? DeliveredAt { get; init; }
    public DateTimeOffset? RespondedAt { get; init; }
    public string? RejectionCode { get; init; }
    public string? RejectionMessage { get; init; }

    public static EdgeCommandAckResult FromCommand(EdgeCommand command)
    {
        return new EdgeCommandAckResult
        {
            CommandId = command.Id,
            Status = command.Status.ToString(),
            DeliveredAt = command.DeliveredAt,
            RespondedAt = command.RespondedAt,
            RejectionCode = command.RejectionCode,
            RejectionMessage = command.RejectionMessage
        };
    }
}
