namespace Domain.Common;

public abstract class RobotConfigurationEntity : BusinessEntity, IRobotSyncEntity
{
    public Guid OriginNodeId { get; set; }

    public long Version { get; set; }

    public DateTimeOffset? SyncedAt { get; set; }
}
