using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Organizations.Requests;
using Application.Tenants.Organizations.Results;
using Domain.Common;
using Domain.Common.Enums;
using Domain.Tenants.Entities;

namespace Application.Tenants.Organizations.Commands;

public sealed class OrganizationLifecycleTransitionCommandHandler
{
    private readonly IOrganizationStore _organizations;

    public OrganizationLifecycleTransitionCommandHandler(IOrganizationStore organizations)
    {
        _organizations = organizations;
    }

    public Task<ApiResult<OrganizationResult>> HandleAsync(
        OrganizationLifecycleTransitionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!OrganizationAccessRules.CanManageOrganizationLifecycle(command.UserContext))
        {
            return Task.FromResult(ApiResult<OrganizationResult>.Fail(
                "Only system administrators can change the organization operational lifecycle.", 403));
        }

        var requestError = Validate(command.Action, command.Request);
        if (requestError is not null)
        {
            return Task.FromResult(ApiResult<OrganizationResult>.Fail(requestError, 400));
        }

        return _organizations.ExecuteInTransactionAsync(async () =>
        {
            await _organizations.AcquireLifecycleMutationLockAsync(
                command.OrganizationId,
                cancellationToken);

            var request = command.Request;
            var idempotencyKey = request.IdempotencyKey?.Trim();
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existing = await _organizations.GetStatusTransitionByIdempotencyKeyAsync(
                    command.OrganizationId,
                    idempotencyKey,
                    cancellationToken);
                if (existing is not null)
                {
                    if (!Matches(existing, command.Action, request))
                    {
                        return ApiResult<OrganizationResult>.Fail(
                            "Idempotency key was already used for a different organization lifecycle transition.", 409);
                    }

                    var existingOrganization = await _organizations.GetByIdAsync(
                        command.OrganizationId,
                        asNoTracking: true,
                        cancellationToken);
                    return existingOrganization is null
                        ? ApiResult<OrganizationResult>.Fail("Organization not found.", 404)
                        : ApiResult<OrganizationResult>.Success(
                            OrganizationResultMapper.ToResult(existingOrganization),
                            "Organization lifecycle transition was already applied.");
                }
            }

            var organization = await _organizations.GetByIdAsync(
                command.OrganizationId,
                asNoTracking: false,
                cancellationToken);
            if (organization is null)
            {
                return ApiResult<OrganizationResult>.Fail("Organization not found.", 404);
            }

            try
            {
                var transition = ApplyTransition(organization, command);
                await _organizations.AddStatusTransitionAsync(transition, cancellationToken);
                await _organizations.SaveChangesAsync(cancellationToken);
                return ApiResult<OrganizationResult>.Success(
                    OrganizationResultMapper.ToResult(organization),
                    $"Organization {ToOperationText(command.Action)} successfully.");
            }
            catch (DomainRuleException exception)
            {
                return ApiResult<OrganizationResult>.Fail(exception.Message, 409);
            }
        }, cancellationToken);
    }

    private static OrganizationStatusTransition ApplyTransition(
        Organization organization,
        OrganizationLifecycleTransitionCommand command)
    {
        var request = command.Request;
        var now = DateTimeOffset.UtcNow;
        return command.Action switch
        {
            OrganizationLifecycleAction.Suspend => organization.Suspend(
                command.UserContext.AccountId,
                request.ReasonCode!,
                request.Reason!,
                request.ExpectedRevision,
                request.IdempotencyKey,
                now),
            OrganizationLifecycleAction.Resume => organization.Resume(
                command.UserContext.AccountId,
                request.Reason!,
                request.ExpectedRevision,
                request.IdempotencyKey,
                now),
            OrganizationLifecycleAction.Deactivate => organization.Deactivate(
                command.UserContext.AccountId,
                request.Reason!,
                request.ExpectedRevision,
                request.IdempotencyKey,
                now),
            OrganizationLifecycleAction.Reactivate => organization.Reactivate(
                command.UserContext.AccountId,
                request.Reason!,
                request.ExpectedRevision,
                request.IdempotencyKey,
                now,
                request.ReadinessConfirmed),
            _ => throw new DomainRuleException("Invalid organization lifecycle action.")
        };
    }

    private static string? Validate(
        OrganizationLifecycleAction action,
        OrganizationLifecycleTransitionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return "Organization lifecycle reason is required.";
        }

        if (action == OrganizationLifecycleAction.Suspend && string.IsNullOrWhiteSpace(request.ReasonCode))
        {
            return "Suspension reason code is required.";
        }

        if (action == OrganizationLifecycleAction.Reactivate && !request.ReadinessConfirmed)
        {
            return "Reactivation requires an explicit operational-readiness confirmation.";
        }

        return null;
    }

    private static bool Matches(
        OrganizationStatusTransition transition,
        OrganizationLifecycleAction action,
        OrganizationLifecycleTransitionRequest request) =>
        transition.ToStatus == ToStatus(action) &&
        MatchesSourceStatus(transition.FromStatus, action) &&
        string.Equals(transition.ReasonCode, action == OrganizationLifecycleAction.Suspend ? request.ReasonCode?.Trim() : action == OrganizationLifecycleAction.Deactivate ? "ServiceEnded" : null, StringComparison.Ordinal) &&
        string.Equals(transition.Reason, request.Reason?.Trim(), StringComparison.Ordinal) &&
        transition.ReadinessConfirmed == (action == OrganizationLifecycleAction.Reactivate ? request.ReadinessConfirmed : null);

    private static bool MatchesSourceStatus(EntityStatus sourceStatus, OrganizationLifecycleAction action) => action switch
    {
        OrganizationLifecycleAction.Suspend => sourceStatus == EntityStatus.Active,
        OrganizationLifecycleAction.Resume => sourceStatus == EntityStatus.Suspended,
        OrganizationLifecycleAction.Deactivate => sourceStatus is EntityStatus.Active or EntityStatus.Suspended,
        OrganizationLifecycleAction.Reactivate => sourceStatus == EntityStatus.Inactive,
        _ => false
    };

    private static EntityStatus ToStatus(OrganizationLifecycleAction action) => action switch
    {
        OrganizationLifecycleAction.Suspend => EntityStatus.Suspended,
        OrganizationLifecycleAction.Deactivate => EntityStatus.Inactive,
        OrganizationLifecycleAction.Resume or OrganizationLifecycleAction.Reactivate => EntityStatus.Active,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };

    private static string ToOperationText(OrganizationLifecycleAction action) => action switch
    {
        OrganizationLifecycleAction.Suspend => "suspended",
        OrganizationLifecycleAction.Resume => "resumed",
        OrganizationLifecycleAction.Deactivate => "deactivated",
        OrganizationLifecycleAction.Reactivate => "reactivated",
        _ => "updated"
    };
}
