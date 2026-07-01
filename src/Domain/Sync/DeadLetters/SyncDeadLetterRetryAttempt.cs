using Domain.Common;
using Domain.Identity.Entities;

namespace Domain.Sync.DeadLetters;

public class SyncDeadLetterRetryAttempt : AppendOnlyEntity
{
    public Guid SyncDeadLetterId { get; private set; }
    public int AttemptNumber { get; private set; }
    public Guid RequestedByAccountId { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public string Reason { get; private set; } = null!;
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool? Succeeded { get; private set; }
    public string? ResultMessage { get; private set; }
    public virtual SyncDeadLetter SyncDeadLetter { get; private set; } = null!;
    public virtual Account RequestedByAccount { get; private set; } = null!;

    private SyncDeadLetterRetryAttempt() { }

    public static SyncDeadLetterRetryAttempt Create(Guid deadLetterId, int attemptNumber, Guid actorId, DateTimeOffset at, string reason) =>
        new()
        {
            SyncDeadLetterId = deadLetterId,
            AttemptNumber = attemptNumber,
            RequestedByAccountId = actorId,
            RequestedAt = at,
            Reason = reason.Trim()
        };

    public void Complete(bool succeeded, DateTimeOffset at, string message)
    {
        Succeeded = succeeded;
        CompletedAt = at;
        ResultMessage = message.Length <= 1000 ? message : message[..1000];
    }
}
