namespace Domain.Common;

public abstract class CatalogEntity : LongEntity, IAuditable
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? CreatedByAccountId { get; set; }
    public Guid? UpdatedByAccountId { get; set; }
}
