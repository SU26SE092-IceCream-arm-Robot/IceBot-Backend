namespace Domain.Common;

public abstract class RobotRuntimeAggregateEntity : GuidEntity, IAuditable, ISoftDeletable, IRobotSyncEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? CreatedByAccountId { get; set; }
    public Guid? UpdatedByAccountId { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByAccountId { get; set; }

    public Guid OriginNodeId { get; set; }
    public long Version { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
}
