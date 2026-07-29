using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Identity.Tokens.Claims;
using Application.ProductionPackages.Installation;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks.Commands;
using Application.Tenants.Stores;
using Application.Tenants.Stores.Commands;
using Domain.Common;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace Application.Tenants.Onboarding;

public sealed class FranchiseOnboardingService(
    IFranchiseOnboardingStore store,
    CreateStoreCommandHandler createStore,
    CreateKioskCommandHandler createKiosk,
    ProductionPackageInstallationService installPackage)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan RunningLease = TimeSpan.FromMinutes(5);

    public async Task<ApiResult<FranchiseOnboardingResult>> StartAsync(
        StartFranchiseOnboardingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<FranchiseOnboardingResult>.Fail("Access denied.", 403);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            return ApiResult<FranchiseOnboardingResult>.Fail("Idempotency-Key is required.", 400);
        if (command.IdempotencyKey.Trim().Length > 200)
            return ApiResult<FranchiseOnboardingResult>.Fail("Idempotency-Key cannot exceed 200 characters.", 400);
        if (command.Request.Store is null || command.Request.Kiosk is null)
            return ApiResult<FranchiseOnboardingResult>.Fail("Store and kiosk onboarding requests are required.", 400);
        if (command.Request.ProductionPackageId.HasValue != command.Request.ProductionPackageVersionId.HasValue)
            return ApiResult<FranchiseOnboardingResult>.Fail(
                "Production package and version must either both be provided or both be omitted.", 400);

        var requestJson = JsonSerializer.Serialize(command.Request, JsonOptions);
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestJson)));
        var key = command.IdempotencyKey.Trim();
        var existing = await store.FindByIdempotencyKeyAsync(command.OrganizationId, key, cancellationToken);
        if (existing is not null)
        {
            if (!existing.Matches(checksum))
                return ApiResult<FranchiseOnboardingResult>.Fail(
                    "Idempotency key was already used with a different onboarding payload.", 409);
            return await ResumeAsync(command.UserContext, command.OrganizationId, existing.Id, cancellationToken);
        }

        var onboarding = FranchiseOnboarding.Start(
            command.OrganizationId,
            key,
            checksum,
            requestJson,
            command.UserContext.AccountId,
            DateTimeOffset.UtcNow);
        if (!await store.TryInsertAsync(onboarding, cancellationToken))
        {
            existing = await store.FindByIdempotencyKeyAsync(command.OrganizationId, key, cancellationToken)
                ?? throw new InvalidOperationException("Concurrent onboarding insert did not produce a winner.");
            if (!existing.Matches(checksum))
                return ApiResult<FranchiseOnboardingResult>.Fail(
                    "Idempotency key was concurrently used with a different onboarding payload.", 409);
            onboarding = existing;
        }

        return await ResumeAsync(command.UserContext, command.OrganizationId, onboarding.Id, cancellationToken);
    }

    public async Task<ApiResult<FranchiseOnboardingResult>> ResumeAsync(
        CurrentUserContext user,
        Guid organizationId,
        Guid onboardingId,
        CancellationToken cancellationToken = default)
    {
        if (!CanManage(user, organizationId))
            return ApiResult<FranchiseOnboardingResult>.Fail("Access denied.", 403);
        var snapshot = await store.GetAsync(organizationId, onboardingId, tracked: false, cancellationToken);
        if (snapshot is null)
            return ApiResult<FranchiseOnboardingResult>.Fail("Franchise onboarding not found.", 404);
        if (snapshot.Status == FranchiseOnboardingStatus.ReadyForActivation)
            return ApiResult<FranchiseOnboardingResult>.Success(FranchiseOnboardingResult.From(snapshot));
        if (snapshot.Status == FranchiseOnboardingStatus.Cancelled)
            return ApiResult<FranchiseOnboardingResult>.Fail("Cancelled onboarding cannot be resumed.", 409);

        var now = DateTimeOffset.UtcNow;
        if (!await store.TryClaimAsync(
                organizationId, onboardingId, now, now - RunningLease, cancellationToken))
        {
            var current = await store.GetAsync(organizationId, onboardingId, tracked: false, cancellationToken)
                ?? throw new InvalidOperationException("Onboarding disappeared while claiming execution.");
            return current.Status == FranchiseOnboardingStatus.ReadyForActivation
                ? ApiResult<FranchiseOnboardingResult>.Success(FranchiseOnboardingResult.From(current))
                : ApiResult<FranchiseOnboardingResult>.Fail("Franchise onboarding is already running.", 409);
        }

        var onboarding = await store.GetAsync(organizationId, onboardingId, tracked: true, cancellationToken)
            ?? throw new InvalidOperationException("Claimed onboarding could not be loaded.");
        StartFranchiseOnboardingRequest request;
        try
        {
            if (onboarding.RequestSchemaVersion != 1)
                throw new DomainRuleException($"Unsupported onboarding request schema version {onboarding.RequestSchemaVersion}.");
            request = JsonSerializer.Deserialize<StartFranchiseOnboardingRequest>(onboarding.RequestJson, JsonOptions)
                ?? throw new DomainRuleException("Onboarding request payload is empty.");
        }
        catch (Exception exception) when (exception is JsonException or DomainRuleException)
        {
            return await FailAsync(onboarding, "INVALID_ONBOARDING_PAYLOAD", exception.Message, cancellationToken);
        }

        if (!onboarding.StoreId.HasValue)
        {
            var storeId = await store.FindStoreIdByCodeAsync(
                organizationId, request.Store.Code.Trim().ToUpperInvariant(), cancellationToken);
            if (!storeId.HasValue)
            {
                var result = await createStore.HandleAsync(new CreateStoreCommand
                {
                    UserContext = user,
                    OrganizationId = organizationId,
                    Request = request.Store
                }, cancellationToken);
                if (!result.Succeeded || result.Data is null)
                    return await FailAsync(onboarding, "STORE_PROVISIONING_FAILED", result.Message, cancellationToken);
                storeId = result.Data.Id;
            }
            onboarding.RecordStore(storeId.Value);
            await store.SaveChangesAsync(cancellationToken);
        }

        if (!onboarding.KioskId.HasValue)
        {
            var kioskId = await store.FindKioskIdByCodeAsync(
                organizationId,
                onboarding.StoreId!.Value,
                request.Kiosk.Code.Trim().ToUpperInvariant(),
                cancellationToken);
            if (!kioskId.HasValue)
            {
                var result = await createKiosk.HandleAsync(new CreateKioskCommand
                {
                    UserContext = user,
                    StoreId = onboarding.StoreId.Value,
                    Request = request.Kiosk
                }, cancellationToken);
                if (!result.Succeeded || result.Data is null)
                    return await FailAsync(onboarding, "KIOSK_PROVISIONING_FAILED", result.Message, cancellationToken);
                kioskId = result.Data.Id;
            }
            onboarding.RecordKiosk(kioskId.Value);
            await store.SaveChangesAsync(cancellationToken);
        }

        if (request.ProductionPackageId.HasValue && !onboarding.PackageInstallationId.HasValue)
        {
            var result = await installPackage.InstallAsync(new InstallProductionPackageCommand
            {
                UserContext = user,
                OrganizationId = organizationId,
                StoreId = onboarding.StoreId,
                KioskId = onboarding.KioskId,
                PackageId = request.ProductionPackageId.Value,
                PackageVersionId = request.ProductionPackageVersionId!.Value,
                IdempotencyKey = $"franchise-onboarding:{onboarding.Id:D}:package",
                ProductSourceKeys = request.ProductSourceKeys
            }, cancellationToken);
            if (!result.Succeeded || result.Data is null)
                return await FailAsync(onboarding, "PACKAGE_INSTALLATION_FAILED", result.Message, cancellationToken);
            onboarding.RecordPackageInstallation(result.Data.Id);
            await store.SaveChangesAsync(cancellationToken);
        }

        onboarding.MarkReady(DateTimeOffset.UtcNow);
        onboarding.UpdatedByAccountId = user.AccountId;
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<FranchiseOnboardingResult>.Success(
            FranchiseOnboardingResult.From(onboarding),
            "Franchise onboarding is ready for explicit activation review.",
            201);
    }

    public async Task<ApiResult<FranchiseOnboardingResult>> GetAsync(
        CurrentUserContext user,
        Guid organizationId,
        Guid onboardingId,
        CancellationToken cancellationToken = default)
    {
        if (!CanManage(user, organizationId))
            return ApiResult<FranchiseOnboardingResult>.Fail("Access denied.", 403);
        var onboarding = await store.GetAsync(organizationId, onboardingId, tracked: false, cancellationToken);
        return onboarding is null
            ? ApiResult<FranchiseOnboardingResult>.Fail("Franchise onboarding not found.", 404)
            : ApiResult<FranchiseOnboardingResult>.Success(FranchiseOnboardingResult.From(onboarding));
    }

    public async Task<PagedResult<FranchiseOnboardingResult>> ListAsync(
        CurrentUserContext user,
        Guid organizationId,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        if (!CanManage(user, organizationId))
            return PagedResult<FranchiseOnboardingResult>.Forbidden("Access denied.", pageNumber, pageSize);
        FranchiseOnboardingStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<FranchiseOnboardingStatus>(status, true, out var value) ||
                !Enum.IsDefined(value))
                return PagedResult<FranchiseOnboardingResult>.Fail(
                    "Invalid franchise onboarding status.", 400, pageNumber, pageSize);
            parsedStatus = value;
        }

        var total = await store.CountAsync(organizationId, parsedStatus, cancellationToken);
        var rows = await store.ListAsync(
            organizationId, parsedStatus, pageNumber, pageSize, cancellationToken);
        return PagedResult<FranchiseOnboardingResult>.Success(
            rows.Select(FranchiseOnboardingResult.From), total, pageNumber, pageSize);
    }

    public async Task<ApiResult<FranchiseOnboardingResult>> CancelAsync(
        CurrentUserContext user,
        Guid organizationId,
        Guid onboardingId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (!CanManage(user, organizationId))
            return ApiResult<FranchiseOnboardingResult>.Fail("Access denied.", 403);
        var onboarding = await store.GetAsync(organizationId, onboardingId, tracked: false, cancellationToken);
        if (onboarding is null)
            return ApiResult<FranchiseOnboardingResult>.Fail("Franchise onboarding not found.", 404);
        if (onboarding.Status == FranchiseOnboardingStatus.Cancelled)
            return ApiResult<FranchiseOnboardingResult>.Success(FranchiseOnboardingResult.From(onboarding));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            return ApiResult<FranchiseOnboardingResult>.Fail("Cancel reason is required and cannot exceed 500 characters.", 400);

        var cancelled = await store.TryCancelAsync(
            organizationId, onboardingId, reason.Trim(), user.AccountId, DateTimeOffset.UtcNow, cancellationToken);
        var current = await store.GetAsync(organizationId, onboardingId, tracked: false, cancellationToken)
            ?? throw new InvalidOperationException("Onboarding disappeared while cancelling.");
        return cancelled || current.Status == FranchiseOnboardingStatus.Cancelled
            ? ApiResult<FranchiseOnboardingResult>.Success(FranchiseOnboardingResult.From(current))
            : ApiResult<FranchiseOnboardingResult>.Fail(
                "Running or ready onboarding cannot be cancelled.", 409);
    }

    private async Task<ApiResult<FranchiseOnboardingResult>> FailAsync(
        FranchiseOnboarding onboarding,
        string code,
        string? message,
        CancellationToken cancellationToken)
    {
        onboarding.MarkFailed(code, string.IsNullOrWhiteSpace(message) ? "Onboarding step failed." : message);
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<FranchiseOnboardingResult>.Fail(onboarding.FailureMessage!, 409)
            .AddDetail("onboarding", FranchiseOnboardingResult.From(onboarding));
    }

    private static bool CanManage(CurrentUserContext user, Guid organizationId) =>
        StoreAccessRules.CanManageOrganizationStores(ScopeRoleSets.StoresManage, user, organizationId);
}
