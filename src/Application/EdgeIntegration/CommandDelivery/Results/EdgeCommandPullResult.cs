using Domain.Sync.Entities;

namespace Application.EdgeIntegration.CommandDelivery.Results;

public sealed class EdgeCommandPullResult
{
    public DateTimeOffset ServerTime { get; init; }
    public IReadOnlyCollection<EdgeCommandResult> Commands { get; init; } = Array.Empty<EdgeCommandResult>();

    public static EdgeCommandPullResult FromCommands(
        DateTimeOffset serverTime,
        IEnumerable<(EdgeCommand Command, string PayloadJson)> commands)
    {
        return new EdgeCommandPullResult
        {
            ServerTime = serverTime,
            Commands = commands.Select(item => EdgeCommandResult.FromEntity(item.Command, item.PayloadJson)).ToArray()
        };
    }
}

public sealed class EdgeCommandResult
{
    public Guid CommandId { get; init; }
    public string CommandType { get; init; } = null!;
    public Guid? OrderId { get; init; }
    public Guid KioskId { get; init; }
    public Guid TargetExecutionEndpointId { get; init; }
    public int? DispatchAttemptNo { get; init; }
    public DateTimeOffset IssuedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string PayloadJson { get; init; } = null!;

    public static EdgeCommandResult FromEntity(EdgeCommand command, string payloadJson)
    {
        return new EdgeCommandResult
        {
            CommandId = command.Id,
            CommandType = command.CommandType.ToString(),
            OrderId = command.OrderId,
            KioskId = command.KioskId,
            TargetExecutionEndpointId = command.TargetExecutionEndpointId,
            DispatchAttemptNo = command.DispatchAttemptNo,
            IssuedAt = command.CreatedAt,
            ExpiresAt = command.CommandExpiryAt,
            PayloadJson = payloadJson
        };
    }
}
