namespace Domain.Common;

public abstract class GuidEntity
{
    public Guid Id { get; set; }
}

public abstract class LongEntity
{
    public long Id { get; set; }
}

public abstract class AppendOnlyEntity : GuidEntity, IAuditable
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? CreatedByAccountId { get; set; }
    public Guid? UpdatedByAccountId { get; set; }
}
