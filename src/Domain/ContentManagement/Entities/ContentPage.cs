using Domain.Common;

namespace Domain.ContentManagement.Entities;

public sealed class ContentPage : BusinessEntity
{
    public string Key { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string DraftTitle { get; private set; } = null!;
    public string DraftBodyHtml { get; private set; } = null!;
    public Guid? PublishedRevisionId { get; private set; }
    public int Revision { get; private set; } = 1;

    private ContentPage() { }

    public static ContentPage Create(string key, string slug, string title, string bodyHtml, Guid actorId, DateTimeOffset now) => new()
    {
        Key = Required(key, 100), Slug = Required(slug, 120), DraftTitle = Required(title, 300), DraftBodyHtml = Required(bodyHtml, 100_000),
        CreatedAt = now, CreatedByAccountId = actorId
    };

    public void UpdateDraft(string title, string bodyHtml, int expectedRevision, Guid actorId, DateTimeOffset now)
    {
        EnsureRevision(expectedRevision);
        DraftTitle = Required(title, 300); DraftBodyHtml = Required(bodyHtml, 100_000);
        UpdatedAt = now; UpdatedByAccountId = actorId; Revision++;
    }

    public ContentPageRevision Publish(Guid actorId, int expectedRevision, DateTimeOffset now)
    {
        EnsureRevision(expectedRevision);
        var published = ContentPageRevision.Create(Id, Revision, DraftTitle, DraftBodyHtml, actorId, now);
        PublishedRevisionId = published.Id;
        UpdatedAt = now; UpdatedByAccountId = actorId; Revision++;
        return published;
    }

    private void EnsureRevision(int expectedRevision)
    {
        if (Revision != expectedRevision) throw new DomainRuleException("The content page was changed by another user. Refresh and try again.");
    }
    private static string Required(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainRuleException("Content value is required.");
        var result = value.Trim();
        if (result.Length > maxLength) throw new DomainRuleException($"Content value cannot exceed {maxLength} characters.");
        return result;
    }
}

public sealed class ContentPageRevision : GuidEntity
{
    public Guid ContentPageId { get; private set; }
    public int RevisionNumber { get; private set; }
    public string Title { get; private set; } = null!;
    public string BodyHtml { get; private set; } = null!;
    public Guid PublishedByAccountId { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }

    private ContentPageRevision() { }

    internal static ContentPageRevision Create(Guid pageId, int revisionNumber, string title, string bodyHtml, Guid actorId, DateTimeOffset now) => new()
    {
        ContentPageId = pageId, RevisionNumber = revisionNumber, Title = title, BodyHtml = bodyHtml,
        PublishedByAccountId = actorId, PublishedAt = now
    };
}
