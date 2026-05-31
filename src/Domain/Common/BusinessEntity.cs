namespace Domain.Common;

public abstract class BusinessEntity : GuidEntity, IAuditable, ISoftDeletable
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? CreatedByAccountId { get; set; }
    public Guid? UpdatedByAccountId { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByAccountId { get; set; }
}
