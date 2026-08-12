using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.ServiceRegistration.Abstractions;
using Application.Shared.Wrappers;
using Domain.Common;
using ServiceRegistrationEntity = Domain.ServiceRegistration.Entities.ServiceRegistration;
using Domain.ServiceRegistration.Enums;

namespace Application.ServiceRegistration;

public sealed class ServiceRegistrationService(
    IServiceRegistrationStore store,
    IServiceRegistrationProvisioner provisioner)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApiResult<ServiceRegistrationReceiptResult>> SubmitAsync(
        string? idempotencyKey, SubmitServiceRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var validation = ValidateSubmission(idempotencyKey, request);
        if (validation is not null) return ApiResult<ServiceRegistrationReceiptResult>.Fail(validation, 400);
        if (!await store.PrivacyPolicyRevisionIsPublishedAsync(request.PrivacyPolicyRevisionId, cancellationToken))
            return ApiResult<ServiceRegistrationReceiptResult>.Fail("The selected privacy policy is not published.", 400);

        var key = idempotencyKey!.Trim();
        var canonical = JsonSerializer.Serialize(request, JsonOptions);
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var existing = await store.FindByIdempotencyKeyAsync(key, cancellationToken);
        if (existing is not null)
        {
            if (!existing.Matches(checksum)) return ApiResult<ServiceRegistrationReceiptResult>.Fail("Idempotency key was already used with a different submission.", 409);
            return ApiResult<ServiceRegistrationReceiptResult>.Success(ToReceipt(existing), "Service registration already submitted.");
        }

        var now = DateTimeOffset.UtcNow;
        var registration = ServiceRegistrationEntity.Submit(
            $"SR-{now:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}", key, checksum,
            request.ContactName, request.Email.Trim(), request.Email.Trim().ToLowerInvariant(), request.PhoneNumber,
            NormalizePhone(request.PhoneNumber), request.BusinessName, request.LegalName, request.TaxCode,
            request.Address, request.ExpectedLocationCount, request.Message, request.PrivacyPolicyRevisionId, now);
        if (!await store.TryAddAsync(registration, cancellationToken))
        {
            existing = await store.FindByIdempotencyKeyAsync(key, cancellationToken)
                ?? throw new InvalidOperationException("Concurrent service registration insert did not produce a winner.");
            if (!existing.Matches(checksum)) return ApiResult<ServiceRegistrationReceiptResult>.Fail("Idempotency key was already used with a different submission.", 409);
            return ApiResult<ServiceRegistrationReceiptResult>.Success(ToReceipt(existing), "Service registration already submitted.");
        }
        return ApiResult<ServiceRegistrationReceiptResult>.Success(ToReceipt(registration), "Service registration submitted.", 201);
    }

    public async Task<PagedResult<ServiceRegistrationResult>> ListAsync(ServiceRegistrationManagementQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.PageNumber, 1); var size = Math.Clamp(query.PageSize, 1, 100);
        if (!ServiceRegistrationPermissionRules.CanManage(query.UserContext)) return PagedResult<ServiceRegistrationResult>.Forbidden("Access denied.", page, size);
        if (!TryParseStatus(query.Status, out var status)) return PagedResult<ServiceRegistrationResult>.Fail("Invalid service registration status.", 400, page, size);
        var total = await store.CountAsync(query.Search, status, query.CreatedFrom, query.CreatedTo, cancellationToken);
        var rows = await store.ListAsync(query.Search, status, query.CreatedFrom, query.CreatedTo, page, size, cancellationToken);
        return PagedResult<ServiceRegistrationResult>.Success(rows.Select(ToResult), total, page, size);
    }

    public async Task<ApiResult<ServiceRegistrationResult>> GetAsync(Application.Identity.Tokens.Claims.CurrentUserContext user, Guid id, CancellationToken cancellationToken = default)
    {
        if (!ServiceRegistrationPermissionRules.CanManage(user)) return ApiResult<ServiceRegistrationResult>.Fail("Access denied.", 403);
        var registration = await store.GetAsync(id, false, cancellationToken);
        return registration is null ? ApiResult<ServiceRegistrationResult>.Fail("Service registration not found.", 404) : ApiResult<ServiceRegistrationResult>.Success(ToResult(registration));
    }

    public Task<ApiResult<ServiceRegistrationResult>> StartReviewAsync(Application.Identity.Tokens.Claims.CurrentUserContext user, Guid id, int expectedRevision, CancellationToken ct = default) => MutateAsync(user, id, registration => registration.StartReview(user.AccountId, expectedRevision, DateTimeOffset.UtcNow), ct);
    public Task<ApiResult<ServiceRegistrationResult>> RejectAsync(Application.Identity.Tokens.Claims.CurrentUserContext user, Guid id, ChangeServiceRegistrationStateRequest request, CancellationToken ct = default) => MutateAsync(user, id, registration => registration.Reject(user.AccountId, request.Reason ?? string.Empty, request.ExpectedRevision, DateTimeOffset.UtcNow), ct);

    public async Task<ApiResult<ServiceRegistrationResult>> ApproveAsync(Application.Identity.Tokens.Claims.CurrentUserContext user, Guid id, ServiceRegistrationProvisioningRequest request, CancellationToken ct = default)
    {
        if (!ServiceRegistrationPermissionRules.CanManage(user)) return ApiResult<ServiceRegistrationResult>.Fail("Access denied.", 403);
        var outcome = await provisioner.ProvisionAsync(id, user.AccountId, request, false, ct);
        return outcome.Succeeded && outcome.Registration is not null
            ? ApiResult<ServiceRegistrationResult>.Success(ToResult(outcome.Registration), outcome.Message, outcome.StatusCode)
            : ApiResult<ServiceRegistrationResult>.Fail(outcome.Message, outcome.StatusCode);
    }

    public async Task<ApiResult<ServiceRegistrationResult>> RetryProvisioningAsync(Application.Identity.Tokens.Claims.CurrentUserContext user, Guid id, ChangeServiceRegistrationStateRequest request, CancellationToken ct = default)
    {
        if (!ServiceRegistrationPermissionRules.CanManage(user)) return ApiResult<ServiceRegistrationResult>.Fail("Access denied.", 403);
        var existing = await store.GetAsync(id, false, ct);
        if (existing is null) return ApiResult<ServiceRegistrationResult>.Fail("Service registration not found.", 404);
        if (existing.Status != ServiceRegistrationStatus.ProvisioningFailed || string.IsNullOrWhiteSpace(existing.ApprovedProvisioningJson)) return ApiResult<ServiceRegistrationResult>.Fail("Only failed provisioning can be retried.", 409);
        if (existing.Revision != request.ExpectedRevision) return ApiResult<ServiceRegistrationResult>.Fail("The service registration was changed by another user. Refresh and try again.", 409);
        var provisioning = JsonSerializer.Deserialize<ServiceRegistrationProvisioningRequest>(existing.ApprovedProvisioningJson, JsonOptions);
        if (provisioning is null) return ApiResult<ServiceRegistrationResult>.Fail("Approved provisioning data is invalid.", 500);
        var retryRequest = new ServiceRegistrationProvisioningRequest
        {
            OrganizationCode = provisioning.OrganizationCode,
            OrganizationName = provisioning.OrganizationName,
            OrganizationLegalName = provisioning.OrganizationLegalName,
            OrganizationTaxCode = provisioning.OrganizationTaxCode,
            AdminUserName = provisioning.AdminUserName,
            AdminEmail = provisioning.AdminEmail,
            AdminFullName = provisioning.AdminFullName,
            LocalLoginEnabled = provisioning.LocalLoginEnabled,
            GoogleLoginEnabled = provisioning.GoogleLoginEnabled,
            ExpectedRevision = request.ExpectedRevision
        };
        var outcome = await provisioner.ProvisionAsync(id, user.AccountId, retryRequest, true, ct);
        return outcome.Succeeded && outcome.Registration is not null
            ? ApiResult<ServiceRegistrationResult>.Success(ToResult(outcome.Registration), outcome.Message, outcome.StatusCode)
            : ApiResult<ServiceRegistrationResult>.Fail(outcome.Message, outcome.StatusCode);
    }

    private async Task<ApiResult<ServiceRegistrationResult>> MutateAsync(Application.Identity.Tokens.Claims.CurrentUserContext user, Guid id, Action<ServiceRegistrationEntity> mutation, CancellationToken ct)
    {
        if (!ServiceRegistrationPermissionRules.CanManage(user)) return ApiResult<ServiceRegistrationResult>.Fail("Access denied.", 403);
        var registration = await store.GetAsync(id, true, ct);
        if (registration is null) return ApiResult<ServiceRegistrationResult>.Fail("Service registration not found.", 404);
        try { mutation(registration); await store.SaveChangesAsync(ct); return ApiResult<ServiceRegistrationResult>.Success(ToResult(registration)); }
        catch (DomainRuleException ex) { return ApiResult<ServiceRegistrationResult>.Fail(ex.Message, 409); }
    }

    private static string? ValidateSubmission(string? key, SubmitServiceRegistrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length > 200) return "Idempotency-Key is required and cannot exceed 200 characters.";
        if (string.IsNullOrWhiteSpace(request.ContactName) || string.IsNullOrWhiteSpace(request.BusinessName) || string.IsNullOrWhiteSpace(request.Email)) return "Contact name, business name, and email are required.";
        if (!request.Email.Contains('@', StringComparison.Ordinal) || !request.PrivacyPolicyAccepted || request.PrivacyPolicyRevisionId == Guid.Empty) return "A valid email and privacy policy acceptance are required.";
        if (request.ExpectedLocationCount is < 1 or > 10_000) return "Expected location count must be between 1 and 10000.";
        return null;
    }
    private static string? NormalizePhone(string? phone) => string.IsNullOrWhiteSpace(phone) ? null : new string(phone.Where(char.IsDigit).ToArray());
    private static bool TryParseStatus(string? value, out ServiceRegistrationStatus? status) { status = null; if (string.IsNullOrWhiteSpace(value)) return true; if (!Enum.TryParse<ServiceRegistrationStatus>(value, true, out var parsed) || !Enum.IsDefined(parsed)) return false; status = parsed; return true; }
    private static ServiceRegistrationReceiptResult ToReceipt(ServiceRegistrationEntity value) => new() { Id = value.Id, ReferenceCode = value.ReferenceCode, Status = value.Status, SubmittedAt = value.CreatedAt };
    internal static ServiceRegistrationResult ToResult(ServiceRegistrationEntity x) => new() { Id=x.Id, ReferenceCode=x.ReferenceCode, ContactName=x.ContactName, Email=x.Email, PhoneNumber=x.PhoneNumber, BusinessName=x.BusinessName, LegalName=x.LegalName, TaxCode=x.TaxCode, Address=x.Address, ExpectedLocationCount=x.ExpectedLocationCount, Message=x.Message, PrivacyPolicyRevisionId=x.PrivacyPolicyRevisionId, Status=x.Status, ReviewReason=x.ReviewReason, ReviewedByAccountId=x.ReviewedByAccountId, ReviewedAt=x.ReviewedAt, ProvisionedOrganizationId=x.ProvisionedOrganizationId, ProvisionedOrgAdminAccountId=x.ProvisionedOrgAdminAccountId, ProvisionedInvitationId=x.ProvisionedInvitationId, ProvisioningFailureCode=x.ProvisioningFailureCode, ProvisioningFailureMessage=x.ProvisioningFailureMessage, Revision=x.Revision, CreatedAt=x.CreatedAt, UpdatedAt=x.UpdatedAt };
}
