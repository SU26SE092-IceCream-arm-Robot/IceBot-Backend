using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.ContentManagement;
using Application.Email;
using Application.ServiceRegistration;
using Application.ServiceRegistration.Abstractions;
using Domain.Common.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.ServiceRegistration.Entities;
using Domain.ServiceRegistration.Enums;
using ServiceRegistrationEntity = Domain.ServiceRegistration.Entities.ServiceRegistration;
using Domain.Tenants.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.ServiceRegistration;

public sealed class ServiceRegistrationStore(IceBotDbContext db) : IServiceRegistrationStore
{
    public Task<ServiceRegistrationEntity?> FindByIdempotencyKeyAsync(string key, CancellationToken ct = default) =>
        db.ServiceRegistrations.AsNoTracking().FirstOrDefaultAsync(x => x.IdempotencyKey == key, ct);

    public Task<ServiceRegistrationEntity?> GetAsync(Guid id, bool tracked, CancellationToken ct = default)
    {
        var query = tracked ? db.ServiceRegistrations.AsQueryable() : db.ServiceRegistrations.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<bool> PrivacyPolicyRevisionIsPublishedAsync(Guid revisionId, CancellationToken ct = default) =>
        db.ContentPages.AnyAsync(x => x.PublishedRevisionId == revisionId, ct);

    public async Task<bool> TryAddAsync(ServiceRegistrationEntity registration, CancellationToken ct = default)
    {
        try { await db.ServiceRegistrations.AddAsync(registration, ct); await db.SaveChangesAsync(ct); return true; }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return false; }
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public Task<int> CountAsync(string? search, ServiceRegistrationStatus? status, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default) =>
        Apply(db.ServiceRegistrations.AsNoTracking(), search, status, from, to).CountAsync(ct);

    public async Task<IReadOnlyList<ServiceRegistrationEntity>> ListAsync(string? search, ServiceRegistrationStatus? status, DateTimeOffset? from, DateTimeOffset? to, int page, int size, CancellationToken ct = default) =>
        await Apply(db.ServiceRegistrations.AsNoTracking(), search, status, from, to)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);

    private static IQueryable<ServiceRegistrationEntity> Apply(IQueryable<ServiceRegistrationEntity> query, string? search, ServiceRegistrationStatus? status, DateTimeOffset? from, DateTimeOffset? to)
    {
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAt <= to.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.ReferenceCode.ToLower().Contains(term) || x.NormalizedEmail.Contains(term) || x.BusinessName.ToLower().Contains(term));
        }
        return query;
    }
}

public sealed class ServiceRegistrationProvisioner(
    IceBotDbContext db,
    IEmailSender emailSender,
    IOptions<EmailOptions> emailOptions,
    ILogger<ServiceRegistrationProvisioner> logger) : IServiceRegistrationProvisioner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ServiceRegistrationProvisioningOutcome> ProvisionAsync(Guid registrationId, Guid actorId, ServiceRegistrationProvisioningRequest request, bool retry, CancellationToken ct = default)
    {
        if (!Validate(request, out var error)) return new(false, 400, error!, null, null, false);
        ServiceRegistrationEntity? completed = null;
        AccountInvitation? invitation = null;
        Account? account = null;
        string? rawToken = null;
        try
        {
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var registration = await db.ServiceRegistrations.FirstOrDefaultAsync(x => x.Id == registrationId, ct)
                    ?? throw new ProvisioningException(404, "Service registration not found.");
                if (registration.Status == ServiceRegistrationStatus.Provisioned)
                {
                    completed = registration;
                    await transaction.CommitAsync(ct);
                    return;
                }
                var approvedJson = JsonSerializer.Serialize(request, JsonOptions);
                registration.BeginProvisioning(actorId, approvedJson, request.ExpectedRevision, DateTimeOffset.UtcNow, retry);

                var organizationCode = request.OrganizationCode.Trim().ToUpperInvariant();
                var adminEmail = request.AdminEmail.Trim().ToLowerInvariant();
                var adminUserName = NormalizeUserName(request.AdminUserName);
                if (await db.Organizations.WhereNotDeleted().AnyAsync(x => x.Code == organizationCode, ct))
                    throw new ProvisioningException(409, $"Organization code '{organizationCode}' already exists.");
                if (await db.Accounts.WhereNotDeleted().AnyAsync(x => x.Email == adminEmail || x.UserName == adminUserName, ct))
                    throw new ProvisioningException(409, "The initial administrator email or username already exists.");
                var orgAdmin = await db.Roles.FirstOrDefaultAsync(x => x.Code == "OrgAdmin" && x.IsActive, ct)
                    ?? throw new ProvisioningException(500, "The OrgAdmin role is not available.");

                var now = DateTimeOffset.UtcNow;
                var organization = new Organization
                {
                    Code = organizationCode, Name = request.OrganizationName.Trim(), LegalName = TrimOrNull(request.OrganizationLegalName),
                    TaxCode = TrimOrNull(request.OrganizationTaxCode), Email = adminEmail, PhoneNumber = registration.PhoneNumber,
                    Address = registration.Address, Status = EntityStatus.Active, CreatedAt = now, CreatedByAccountId = actorId
                };
                account = new Account
                {
                    UserName = adminUserName, Email = adminEmail, FullName = TrimOrNull(request.AdminFullName) ?? registration.ContactName,
                    Status = AccountStatus.Invited, LocalLoginEnabled = request.LocalLoginEnabled, GoogleLoginEnabled = request.GoogleLoginEnabled,
                    GoogleEmail = request.GoogleLoginEnabled ? adminEmail : null, CreatedAt = now, CreatedByAccountId = actorId
                };
                account.AccountRoles.Add(new AccountRole { RoleId = orgAdmin.Id, Role = orgAdmin, OrganizationId = organization.Id, AssignedAt = now, AssignedByAccountId = actorId });
                rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                invitation = new AccountInvitation
                {
                    AccountId = account.Id, TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))),
                    InvitedAt = now, ExpiresAt = now.AddDays(7), InvitedByAccountId = actorId, Purpose = "AccountInvitation"
                };
                await db.Organizations.AddAsync(organization, ct);
                await db.Accounts.AddAsync(account, ct);
                await db.AccountInvitations.AddAsync(invitation, ct);
                registration.CompleteProvisioning(organization.Id, account.Id, invitation.Id, now);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                completed = registration;
            });
        }
        catch (ProvisioningException ex)
        {
            await RecordFailureAsync(registrationId, ex.Message, ex.StatusCode == 409 ? "PROVISIONING_CONFLICT" : "PROVISIONING_FAILED", ct);
            return new(false, ex.StatusCode, ex.Message, null, null, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Service registration provisioning failed for {RegistrationId}", registrationId);
            await RecordFailureAsync(registrationId, "Provisioning failed. Resolve the conflict or retry.", "PROVISIONING_FAILED", ct);
            return new(false, 500, "Provisioning failed. Resolve the conflict or retry.", null, null, false);
        }

        if (completed is null) return new(false, 500, "Provisioning did not return a result.", null, null, false);
        if (invitation is null || account is null || rawToken is null) return new(true, 200, "Service registration was already provisioned.", completed, null, false);
        var invitationUrl = BuildInvitationUrl(rawToken);
        var emailSent = false;
        if (invitationUrl is not null)
        {
            try
            {
                await emailSender.SendAsync(account.Email, "Complete your IceBot account setup", $"<p>Hello {System.Net.WebUtility.HtmlEncode(account.FullName)},</p><p>Complete your account setup: <a href=\"{System.Net.WebUtility.HtmlEncode(invitationUrl)}\">Accept invitation</a></p>", ct);
                invitation.EmailSentAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                emailSent = true;
            }
            catch (Exception ex) { logger.LogError(ex, "Invitation delivery failed after provisioning service registration {RegistrationId}", registrationId); }
        }
        return new(true, 201, emailSent ? "Service registration provisioned and invitation email sent." : "Service registration provisioned. Invitation delivery requires retry or manual delivery.", completed, invitationUrl, emailSent);
    }

    private async Task RecordFailureAsync(Guid registrationId, string message, string code, CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        var registration = await db.ServiceRegistrations.FirstOrDefaultAsync(x => x.Id == registrationId, ct);
        if (registration?.Status == ServiceRegistrationStatus.Provisioning)
        {
            registration.RecordProvisioningFailure(code, message, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);
        }
    }

    private string? BuildInvitationUrl(string token) => string.IsNullOrWhiteSpace(emailOptions.Value.InvitationBaseUrl) ? null : $"{emailOptions.Value.InvitationBaseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(token)}";
    private static bool Validate(ServiceRegistrationProvisioningRequest request, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(request.OrganizationCode) || string.IsNullOrWhiteSpace(request.OrganizationName) || string.IsNullOrWhiteSpace(request.AdminUserName) || string.IsNullOrWhiteSpace(request.AdminEmail)) { error = "Organization and initial administrator details are required."; return false; }
        if (!request.LocalLoginEnabled && !request.GoogleLoginEnabled) { error = "At least one administrator login method is required."; return false; }
        if (!request.AdminEmail.Contains('@', StringComparison.Ordinal)) { error = "Initial administrator email is invalid."; return false; }
        return true;
    }
    private static string NormalizeUserName(string value) => new string(value.Trim().ToLowerInvariant().Where(x => char.IsLetterOrDigit(x) || x is '_' or '.' or '-').ToArray());
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private sealed class ProvisioningException(int statusCode, string message) : Exception(message) { public int StatusCode { get; } = statusCode; }
}

public sealed class ContentPageStore(IceBotDbContext db) : IContentPageStore
{
    public async Task<IReadOnlyList<Domain.ContentManagement.Entities.ContentPage>> ListAsync(CancellationToken ct = default) => await db.ContentPages.AsNoTracking().OrderBy(x => x.Key).ToListAsync(ct);
    public Task<Domain.ContentManagement.Entities.ContentPage?> GetByKeyAsync(string key, bool tracked, CancellationToken ct = default) => (tracked ? db.ContentPages.AsQueryable() : db.ContentPages.AsNoTracking()).FirstOrDefaultAsync(x => x.Key == key, ct);
    public async Task<PublishedContentPageResult?> GetPublishedBySlugAsync(string slug, CancellationToken ct = default)
    {
        var row = await (from page in db.ContentPages.AsNoTracking()
                         join revision in db.ContentPageRevisions.AsNoTracking() on page.PublishedRevisionId equals revision.Id
                         where page.Slug == slug
                         select new { page.Slug, revision }).FirstOrDefaultAsync(ct);
        return row is null ? null : new PublishedContentPageResult { Slug = row.Slug, Title = row.revision.Title, BodyHtml = row.revision.BodyHtml, RevisionNumber = row.revision.RevisionNumber, ETag = $"\"content-{row.revision.Id:N}\"", PublishedAt = row.revision.PublishedAt };
    }
    public Task AddAsync(Domain.ContentManagement.Entities.ContentPage page, CancellationToken ct = default) => db.ContentPages.AddAsync(page, ct).AsTask();
    public Task AddRevisionAsync(Domain.ContentManagement.Entities.ContentPageRevision revision, CancellationToken ct = default) => db.ContentPageRevisions.AddAsync(revision, ct).AsTask();
    public async Task SaveChangesAsync(CancellationToken ct = default) => _ = await db.SaveChangesAsync(ct);
}

public sealed class RestrictedContentHtmlSanitizer : IContentHtmlSanitizer
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li", "strong", "em", "b", "i", "blockquote", "table", "thead", "tbody", "tr", "th", "td", "a"
    };
    private static readonly System.Text.RegularExpressions.Regex HtmlComment = new("<!--.*?-->", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex HtmlTag = new("<(?<close>/)?(?<tag>[a-zA-Z0-9]+)(?<attributes>[^>]*)>", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex Href = new("\\bhref\\s*=\\s*(?:\"(?<value>[^\"]*)\"|'(?<value>[^']*)'|(?<value>[^\\s>]+))", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    public string Sanitize(string html)
    {
        var withoutComments = HtmlComment.Replace(html ?? string.Empty, string.Empty);
        return HtmlTag.Replace(withoutComments, match =>
        {
            var tag = match.Groups["tag"].Value.ToLowerInvariant();
            if (!AllowedTags.Contains(tag)) return string.Empty;
            if (match.Groups["close"].Success) return $"</{tag}>";
            if (tag != "a") return $"<{tag}>";

            var href = Href.Match(match.Groups["attributes"].Value);
            if (!href.Success) return "<a>";
            var value = href.Groups["value"].Value.Trim();
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http" or "mailto")) return "<a>";
            return $"<a href=\"{System.Net.WebUtility.HtmlEncode(value)}\" rel=\"noopener noreferrer\">";
        });
    }
}

public static class ServiceRegistrationInfrastructureRegistration
{
    public static IServiceCollection AddServiceRegistrationInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IServiceRegistrationStore, ServiceRegistrationStore>();
        services.AddScoped<IServiceRegistrationProvisioner, ServiceRegistrationProvisioner>();
        services.AddScoped<IContentPageStore, ContentPageStore>();
        services.AddSingleton<IContentHtmlSanitizer, RestrictedContentHtmlSanitizer>();
        return services;
    }
}
