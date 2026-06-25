namespace Domain.Common;

public abstract class GuidEntity
{
    public Guid Id { get; set; } = GuidId.New();
}

public abstract class LongEntity
{
    public long Id { get; set; }
}

public abstract class AuditedEntity : GuidEntity, IAuditable
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? CreatedByAccountId { get; set; }
    public Guid? UpdatedByAccountId { get; set; }
}

public abstract class AppendOnlyEntity : AuditedEntity
{
}

public abstract class AppendOnlySyncEntity : AppendOnlyEntity, IRobotSyncEntity
{
    public Guid OriginNodeId { get; set; }
    public long Version { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
}
