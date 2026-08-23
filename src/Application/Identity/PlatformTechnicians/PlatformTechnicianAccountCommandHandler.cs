using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts;
using Application.Identity.Provisioning;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Domain.Identity.Enums;

namespace Application.Identity.PlatformTechnicians;

public sealed class PlatformTechnicianAccountCommandHandler(
    IIdentityAccountStore accounts,
    TenantAccountCredentialService credentials)
{
    public async Task<ApiResult<TechnicianResult>> CreateAsync(
        CreatePlatformTechnicianRequest request,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email))
        {
            return ApiResult<TechnicianResult>.Fail("User name and email are required.", 400);
        }

        var email = InternalAccountNormalizer.NormalizeEmail(request.Email);
        var userName = InternalAccountNormalizer.NormalizeUserName(request.UserName);
        if (await accounts.ExistsByEmailOrUserNameAsync(email, userName, cancellationToken))
        {
            return ApiResult<TechnicianResult>.Fail("Account already exists.", 409);
        }

        var now = DateTimeOffset.UtcNow;
        var account = new Account
        {
            UserName = userName,
            Email = email,
            FullName = request.FullName?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Status = AccountStatus.Active,
            LocalLoginEnabled = true,
            CreatedAt = now,
            CreatedByAccountId = actorId,
            PlatformTechnicianProfile = new PlatformTechnicianProfile
            {
                CreatedAt = now,
                CreatedByAccountId = actorId
            }
        };
        var credential = credentials.Prepare(account, now);
        await accounts.AddAsync(account, cancellationToken);
        await accounts.SaveChangesAsync(cancellationToken);
        await credentials.TrySendAsync(account, credential, cancellationToken);
        return ApiResult<TechnicianResult>.Success(
            PlatformTechnicianResultMapper.ToResult(account),
            "Technician account created.",
            201);
    }

    public Task<ApiResult<TechnicianResult>> UpdateAsync(
        Guid id,
        UpdatePlatformTechnicianRequest request,
        Guid? actorId,
        CancellationToken cancellationToken) =>
        MutateAsync(
            id,
            request.ExpectedAuthorizationVersion,
            actorId,
            cancellationToken,
            account =>
            {
                account.FullName = request.FullName?.Trim() ?? account.FullName;
                account.PhoneNumber = request.PhoneNumber?.Trim() ?? account.PhoneNumber;
                return null;
            },
            "Technician updated.");

    public Task<ApiResult<TechnicianResult>> LifecycleAsync(
        Guid id,
        TechnicianLifecycleRequest request,
        bool activate,
        Guid? actorId,
        CancellationToken cancellationToken) =>
        MutateAsync(
            id,
            request.ExpectedAuthorizationVersion,
            actorId,
            cancellationToken,
            account =>
            {
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return "Reason is required.";
                }

                if (activate ? account.Status != AccountStatus.Disabled : account.Status != AccountStatus.Active)
                {
                    return "Technician lifecycle transition is invalid.";
                }

                account.Status = activate ? AccountStatus.Active : AccountStatus.Disabled;
                return null;
            },
            activate ? "Technician reactivated." : "Technician deactivated.");

    private Task<ApiResult<TechnicianResult>> MutateAsync(
        Guid id,
        long expectedAuthorizationVersion,
        Guid? actorId,
        CancellationToken cancellationToken,
        Func<Account, string?> mutation,
        string successMessage) =>
        accounts.ExecuteInTransactionAsync(async () =>
        {
            await accounts.AcquireAccountLockAsync(id, cancellationToken);
            var account = await accounts.GetByIdAsync(id, false, cancellationToken);
            if (account?.PlatformTechnicianProfile is null ||
                PlatformTechnicianBoundary.HasMixedActiveRoles(account))
            {
                return ApiResult<TechnicianResult>.Fail("Technician account not found.", 404);
            }

            if (account.AuthorizationVersion != expectedAuthorizationVersion)
            {
                return ApiResult<TechnicianResult>.Fail(
                    "Technician access changed by another user.",
                    409);
            }

            var mutationError = mutation(account);
            if (mutationError is not null)
            {
                return ApiResult<TechnicianResult>.Fail(mutationError, 409);
            }

            account.AuthorizationVersion++;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            account.UpdatedByAccountId = actorId;
            await accounts.SaveChangesAsync(cancellationToken);
            return ApiResult<TechnicianResult>.Success(
                PlatformTechnicianResultMapper.ToResult(account),
                successMessage);
        }, cancellationToken);
}
