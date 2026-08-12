using Application.Shared.Wrappers;
using Domain.Common;
using Domain.ContentManagement.Entities;

namespace Application.ContentManagement;

public sealed class ContentPageService(IContentPageStore store, IContentHtmlSanitizer sanitizer)
{
    public async Task<ApiResult<IEnumerable<ContentPageResult>>> ListAsync(Application.Identity.Tokens.Claims.CurrentUserContext user, CancellationToken ct = default)
    {
        if (!ContentManagementPermissionRules.CanManage(user)) return ApiResult<IEnumerable<ContentPageResult>>.Fail("Access denied.", 403);
        var pages = await store.ListAsync(ct);
        return ApiResult<IEnumerable<ContentPageResult>>.Success(pages.Select(Map));
    }

    public async Task<ApiResult<ContentPageResult>> GetAsync(Application.Identity.Tokens.Claims.CurrentUserContext user, string key, CancellationToken ct = default)
    {
        if (!ContentManagementPermissionRules.CanManage(user)) return ApiResult<ContentPageResult>.Fail("Access denied.", 403);
        var page = await store.GetByKeyAsync(key, false, ct);
        return page is null ? ApiResult<ContentPageResult>.Fail("Content page not found.", 404) : ApiResult<ContentPageResult>.Success(Map(page));
    }

    public async Task<ApiResult<ContentPageResult>> UpdateDraftAsync(Application.Identity.Tokens.Claims.CurrentUserContext user, string key, UpdateContentPageDraftRequest request, CancellationToken ct = default)
    {
        if (!ContentManagementPermissionRules.CanManage(user)) return ApiResult<ContentPageResult>.Fail("Access denied.", 403);
        if (!ContentPageKeys.InitialPages.TryGetValue(key, out var slug)) return ApiResult<ContentPageResult>.Fail("Unsupported content page key.", 400);
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.BodyHtml)) return ApiResult<ContentPageResult>.Fail("Title and content are required.", 400);
        var now = DateTimeOffset.UtcNow;
        var sanitized = sanitizer.Sanitize(request.BodyHtml);
        if (string.IsNullOrWhiteSpace(sanitized)) return ApiResult<ContentPageResult>.Fail("Content has no permitted HTML.", 400);
        var page = await store.GetByKeyAsync(key, true, ct);
        try
        {
            if (page is null)
            {
                if (request.ExpectedRevision != 0) return ApiResult<ContentPageResult>.Fail("Content page does not exist yet.", 409);
                page = ContentPage.Create(key, slug, request.Title, sanitized, user.AccountId, now);
                await store.AddAsync(page, ct);
            }
            else page.UpdateDraft(request.Title, sanitized, request.ExpectedRevision, user.AccountId, now);
            await store.SaveChangesAsync(ct);
            return ApiResult<ContentPageResult>.Success(Map(page));
        }
        catch (DomainRuleException ex) { return ApiResult<ContentPageResult>.Fail(ex.Message, 409); }
    }

    public async Task<ApiResult<ContentPageResult>> PublishAsync(Application.Identity.Tokens.Claims.CurrentUserContext user, string key, int expectedRevision, CancellationToken ct = default)
    {
        if (!ContentManagementPermissionRules.CanManage(user)) return ApiResult<ContentPageResult>.Fail("Access denied.", 403);
        var page = await store.GetByKeyAsync(key, true, ct);
        if (page is null) return ApiResult<ContentPageResult>.Fail("Content page not found.", 404);
        try
        {
            var revision = page.Publish(user.AccountId, expectedRevision, DateTimeOffset.UtcNow);
            await store.AddRevisionAsync(revision, ct);
            await store.SaveChangesAsync(ct);
            return ApiResult<ContentPageResult>.Success(Map(page), "Content page published.");
        }
        catch (DomainRuleException ex) { return ApiResult<ContentPageResult>.Fail(ex.Message, 409); }
    }

    public async Task<PublishedContentPageResult?> GetPublishedAsync(string slug, CancellationToken ct = default) => await store.GetPublishedBySlugAsync(slug, ct);

    private static ContentPageResult Map(ContentPage page) => new() { Id = page.Id, Key = page.Key, Slug = page.Slug, DraftTitle = page.DraftTitle, DraftBodyHtml = page.DraftBodyHtml, PublishedRevisionId = page.PublishedRevisionId, Revision = page.Revision, UpdatedAt = page.UpdatedAt };
}
