namespace Domain.Common;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }

    Guid? CreatedByAccountId { get; set; }
    Guid? UpdatedByAccountId { get; set; }
}

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
    Guid? DeletedByAccountId { get; set; }
}

public interface IRobotSyncEntity
{
    Guid OriginNodeId { get; set; }
    long Version { get; set; }
    DateTimeOffset? SyncedAt { get; set; }
}

public interface IOrganizationScoped
{
    Guid? OrganizationId { get; set; }
}

public interface IStoreScoped : IOrganizationScoped
{
    Guid? StoreId { get; set; }
}

public interface IKioskScoped : IStoreScoped
{
    Guid? KioskId { get; set; }
}
