using Domain.Common;
using Domain.Sync.Enums;

namespace Domain.Sync.Entities;

public class EdgeCommandDeliveryAttempt : AppendOnlyEntity
{
    public Guid EdgeCommandId { get; private set; }
    public int DeliveryAttemptNo { get; private set; }
    public DateTimeOffset SentAt { get; private set; }
    public EdgeCommandDeliveryOutcome Outcome { get; private set; }
    public string? ResponseCode { get; private set; }
    public string? ResponseMessage { get; private set; }
    public virtual EdgeCommand EdgeCommand { get; private set; } = null!;

    private EdgeCommandDeliveryAttempt()
    {
    }

    internal static EdgeCommandDeliveryAttempt Create(
        Guid edgeCommandId,
        int deliveryAttemptNo,
        DateTimeOffset sentAt,
        EdgeCommandDeliveryOutcome outcome,
        string? responseCode,
        string? responseMessage)
    {
        return new EdgeCommandDeliveryAttempt
        {
            EdgeCommandId = edgeCommandId,
            DeliveryAttemptNo = deliveryAttemptNo,
            SentAt = sentAt,
            Outcome = outcome,
            ResponseCode = responseCode,
            ResponseMessage = responseMessage
        };
    }
}
