using Application.Identity.Tokens.Claims;

namespace Application.ContentManagement;

public sealed class UpdateContentPageDraftRequest
{
    public string Title { get; init; } = null!;
    public string BodyHtml { get; init; } = null!;
    public int ExpectedRevision { get; init; }
}

public sealed class PublishContentPageRequest
{
    public int ExpectedRevision { get; init; }
}

public sealed class ContentPageResult
{
    public Guid Id { get; init; }
    public string Key { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string DraftTitle { get; init; } = null!;
    public string DraftBodyHtml { get; init; } = null!;
    public Guid? PublishedRevisionId { get; init; }
    public int Revision { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class PublishedContentPageResult
{
    public string Slug { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string BodyHtml { get; init; } = null!;
    public int RevisionNumber { get; init; }
    public string ETag { get; init; } = null!;
    public DateTimeOffset PublishedAt { get; init; }
}

public interface IContentPageStore
{
    Task<IReadOnlyList<Domain.ContentManagement.Entities.ContentPage>> ListAsync(CancellationToken cancellationToken = default);
    Task<Domain.ContentManagement.Entities.ContentPage?> GetByKeyAsync(string key, bool tracked, CancellationToken cancellationToken = default);
    Task<PublishedContentPageResult?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.ContentManagement.Entities.ContentPage page, CancellationToken cancellationToken = default);
    Task AddRevisionAsync(Domain.ContentManagement.Entities.ContentPageRevision revision, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IContentHtmlSanitizer
{
    string Sanitize(string html);
}

public static class ContentManagementPermissionRules
{
    public static bool CanManage(CurrentUserContext user) => user.IsSystemAdmin;
}

public static class ContentPageKeys
{
    public static readonly IReadOnlyDictionary<string, string> InitialPages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["about-us"] = "about-us",
        ["privacy-policy"] = "privacy-policy",
        ["payment-policy"] = "payment-policy",
        ["terms-of-use"] = "terms-of-use",
        ["contact-information"] = "contact-information"
    };
}
